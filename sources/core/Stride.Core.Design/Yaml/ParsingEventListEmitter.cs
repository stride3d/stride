// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml
{
    public class ParsingEventListEmitter : IEmitter
    {
        private readonly List<ParsingEvent> parsingEvents;

        public ParsingEventListEmitter(List<ParsingEvent> parsingEvents)
        {
            this.parsingEvents = parsingEvents;
        }

        public void Emit(ParsingEvent @event)
        {
            // Ignore some events
            if (@event is StreamStart || @event is StreamEnd
                || @event is DocumentStart || @event is DocumentEnd)
                return;

            parsingEvents.Add(@event);
        }
    }
}
