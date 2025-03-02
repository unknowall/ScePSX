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
            ResetResults(); // 初始化搜索范围为整个数据
        }

        public void UpdateData(byte[] newMemory)
        {
            data = newMemory;
        }

        public void ResetResults()
        {
            results = Enumerable.Range(0, data.Length).ToList();
        }

        // 搜索字节
        public void SearchByte(byte value)
        {
            results = Search((index) => data[index] == value);
        }

        // 搜索字
        public void SearchWord(ushort value)
        {
            results = Search((index) => index + 1 < data.Length && BitConverter.ToUInt16(data, index) == value);
        }

        // 搜索双字
        public void SearchDword(uint value)
        {
            results = Search((index) => index + 3 < data.Length && BitConverter.ToUInt32(data, index) == value);
        }

        // 搜索浮点数
        public void SearchFloat(float value)
        {
            results = Search((index) => index + 3 < data.Length && BitConverter.ToSingle(data, index) == value);
        }

        // 获取当前搜索结果（地址和值）
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

        // 并行化搜索
        private List<int> Search(Func<int, bool> condition)
        {
            var newResults = new List<int>();

            // 并行化搜索
            Parallel.ForEach(Partitioner.Create(0, results.Count), range =>
            {
                var localResults = new List<int>();
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    int index = results[i];
                    if (condition(index))
                        localResults.Add(index);
                }

                lock (newResults) // 合并线程局部结果
                {
                    newResults.AddRange(localResults);
                }
            });

            return newResults;
        }
    }

}
