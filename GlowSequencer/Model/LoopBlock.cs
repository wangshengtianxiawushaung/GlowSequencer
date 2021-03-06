﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer.Model
{
    public class LoopBlock : GroupBlock
    {
        private int _repetitions = 1;

        public int Repetitions { get { return _repetitions; } set { SetProperty(ref _repetitions, Math.Max(1, value)); } }

        public LoopBlock(Timeline timeline)
            : base(timeline)
        {
        }

        internal override IEnumerable<FileSerializer.PrimitiveBlock> BakePrimitive(Track track)
        {
            // only consider children that actually occupy the track
            var relevantChildren = Children.Where(child => child.Tracks.Contains(track)).ToList();

            for (int i = 0; i < _repetitions; i++)

                foreach (var primitive in relevantChildren.SelectMany(child => child.BakePrimitive(track)))
                {
                    primitive.startTime += FileSerializer.PrimitiveBlock.ToTicks(StartTime + i * Duration);
                    primitive.endTime += FileSerializer.PrimitiveBlock.ToTicks(StartTime + i * Duration);
                    yield return primitive;
                }
        }

        public override float GetEndTimeOccupied()
        {
            return StartTime + Duration * _repetitions;
        }

        protected override GloColor GetColorAtLocalTimeCore(float localTime, Track track)
        {
            // De-loop.
            float time = localTime % Duration;

            Block activeBlock = Children
                .Where(b => b.IsTimeInOccupiedRange(time) && b.Tracks.Contains(track))
                .LastOrDefault();

            if (activeBlock != null)
                return activeBlock.GetColorAtTime(time, track);
            else
                return GloColor.Black;
        }

        [Obsolete]
        public override IEnumerable<GloCommand> ToGloCommands(GloSequenceContext context)
        {
            // TO_DO handle repetitions of more than 255
            var loop = new GloLoopCommand(_repetitions);

            GloSequenceContext subContext = new GloSequenceContext(context.Track, loop);
            subContext.Append(Children.Where(child => child.Tracks.Contains(context.Track)));

            if (Duration > subContext.CurrentTime)
                loop.Commands.Add(subContext.Advance(Duration - subContext.CurrentTime).AsCommand());

            subContext.Advance(subContext.CurrentTime * (_repetitions - 1)); // fix internal time
            subContext.Postprocess();

            // advance main context
            context.Advance(subContext.CurrentTime);

            // TO_DO split loop when fractions are involved
            yield return loop;
        }

        public override System.Xml.Linq.XElement ToXML()
        {
            XElement elem = base.ToXML();
            elem.Add(new XElement("repetitions", _repetitions));

            return elem;
        }

        protected override void PopulateFromXML(XElement element)
        {
            base.PopulateFromXML(element);
            Repetitions = (int)element.Element("repetitions");
            // use setter to incorporate sanity check
        }
    }
}
