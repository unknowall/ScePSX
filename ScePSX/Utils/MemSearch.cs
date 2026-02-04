using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScePSX
{

    public class MemorySearch
    {
        private byte[] data;
        public List<int> results;

        public MemorySearch(byte[] memory)
        {
            data = memory;
            ResetResults();
        }

        public void UpdateData(byte[] newMemory)
        {
            data = newMemory;
        }

        public void ResetResults()
        {
            results = Enumerable.Range(0, data.Length).ToList();
        }

        public void SearchByte(byte value)
        {
            results = Search((index) => data[index] == value);
        }

        public void SearchWord(ushort value)
        {
            results = Search((index) => index + 1 < data.Length && BitConverter.ToUInt16(data, index) == value);
        }

        public void SearchDword(uint value)
        {
            results = Search((index) => index + 3 < data.Length && BitConverter.ToUInt32(data, index) == value);
        }

        public void SearchFloat(float value)
        {
            results = Search((index) => index + 3 < data.Length && BitConverter.ToSingle(data, index) == value);
        }

        public List<(int Address, object Value)> GetResults()
        {
            var resultValues = new List<(int, object)>();
            foreach (var index in results)
            {
                if (index < data.Length)
                {
                    resultValues.Add((index, (object)data[index]));
                }
            }
            return resultValues;
        }

        private List<int> Search(Func<int, bool> condition)
        {
            var newResults = new List<int>();

            Parallel.ForEach(Partitioner.Create(0, results.Count), range =>
            {
                var localResults = new List<int>();
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    int index = results[i];
                    if (condition(index))
                        localResults.Add(index);
                }

                lock (newResults)
                {
                    newResults.AddRange(localResults);
                }
            });

            return newResults;
        }
    }

}
