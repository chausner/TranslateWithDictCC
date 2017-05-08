using System;
using System.Collections;
using System.Collections.Generic;

namespace TranslateWithDictCC
{
    class LazyCollection<Source, T> : ICollection<T>, IReadOnlyList<T>, IReadOnlyCollection<T> where T : class
    {
        IList<Source> sources;
        Func<Source, T> generator;

        T[] results;

        public LazyCollection(IList<Source> sources, Func<Source, T> generator)
        {
            this.sources = sources;
            this.generator = generator;

            results = new T[sources.Count];
        }

        public T this[int index]
        {
            get
            {
                T result = results[index];

                if (result == null)
                {
                    result = generator(sources[index]);
                    results[index] = result;
                }

                return result;
            }
        }

        public int Count
        {
            get
            {
                return results.Length;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            GenerateAll();

            return Array.IndexOf(results, item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            GenerateAll();

            results.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new LazyEnumerator(this);
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new LazyEnumerator(this);
        }

        private void GenerateAll()
        {
            for (int i = 0; i < results.Length; i++)
                if (results[i] == null)
                    results[i] = generator(sources[i]);
        }

        private class LazyEnumerator : IEnumerator<T>
        {
            LazyCollection<Source, T> lazyCollection;

            int currentIndex = -1;            

            public LazyEnumerator(LazyCollection<Source, T> lazyCollection)
            {
                this.lazyCollection = lazyCollection;
            }

            public T Current
            {
                get
                {
                    return lazyCollection[currentIndex];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return lazyCollection[currentIndex];
                }
            }

            public bool MoveNext()
            {
                currentIndex++;

                return currentIndex < lazyCollection.results.Length;
            }

            public void Reset()
            {
                currentIndex = -1;
            }

            protected virtual void Dispose(bool disposing)
            {
                lazyCollection = null;
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }
    }
}
