using System.Collections.Generic;
using System.Linq;

namespace VoronoiCS
{
    internal class Queue
    {
        private readonly SortedList<double, Event> _list;

        public Queue()
        {
            _list = new SortedList<double, Event>(new EventYComparer());
        }

        internal void Enqueue(Event @event)
        {
            _list.Add(@event.Point.Y, @event);
        }

        internal Event Dequeue()
        {
            var dequeue = _list.First().Value;
            _list.RemoveAt(0);
            return dequeue;
        }

        internal void Remove(Event @event)
        {
            var index = _list.IndexOfValue(@event);
            if (index != -1)
            {
                _list.RemoveAt(index);
            }
        }

        internal bool IsEmpty()
        {
            return !_list.Any();
        }
    }
}