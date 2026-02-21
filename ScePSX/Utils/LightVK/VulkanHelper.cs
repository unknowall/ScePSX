using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace LightVK
{
    public static class vkStrings
    {
        public static FixedUtf8String AppName { get; } = "LightVK";
        public static FixedUtf8String EngineName { get; } = "VulkanRenderer";
        public static FixedUtf8String VK_KHR_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_surface";
        public static FixedUtf8String VK_KHR_WIN32_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_win32_surface";
        public static FixedUtf8String VK_KHR_XLIB_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_xlib_surface";
        public static FixedUtf8String VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_wayland_surface";
        public static FixedUtf8String VK_KHR_ANDROID_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_android_surface";
        public static FixedUtf8String VK_MVK_MACOS_SURFACE_EXTENSION_NAME { get; } = "VK_MVK_macos_surface";
        public static FixedUtf8String VK_KHR_SWAPCHAIN_EXTENSION_NAME { get; } = "VK_KHR_swapchain";
        public static FixedUtf8String VK_EXT_DEBUG_REPORT_EXTENSION_NAME { get; } = "VK_EXT_debug_report";
        public static FixedUtf8String VK_LAYER_KHRONOS_validation { get; } = "VK_LAYER_KHRONOS_validation";
        public static FixedUtf8String VK_EXT_metal_surface { get; } = "VK_EXT_metal_surface";
        public static FixedUtf8String VK_EXT_descriptor_indexing { get; } = "VK_EXT_descriptor_indexing";
        public static FixedUtf8String VK_KHR_timeline_semaphore { get; } = "VK_KHR_timeline_semaphore";
        public static FixedUtf8String main { get; } = "main";
    }

    public unsafe class FixedUtf8String : IDisposable
    {
        private GCHandle _handle;
        private uint _numBytes;

        public byte* StringPtr => (byte*)_handle.AddrOfPinnedObject().ToPointer();

        public FixedUtf8String(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            var text = Encoding.UTF8.GetBytes(s);
            _handle = GCHandle.Alloc(text, GCHandleType.Pinned);
            _numBytes = (uint)text.Length;
        }

        public void SetText(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            _handle.Free();
            var text = Encoding.UTF8.GetBytes(s);
            _handle = GCHandle.Alloc(text, GCHandleType.Pinned);
            _numBytes = (uint)text.Length;
        }

        private string GetString()
        {
            return Encoding.UTF8.GetString(StringPtr, (int)_numBytes);
        }

        public void Dispose()
        {
            _handle.Free();
        }

        public static implicit operator byte*(FixedUtf8String utf8String) => utf8String.StringPtr;
        public static implicit operator nint(FixedUtf8String utf8String) => new nint(utf8String.StringPtr);
        public static implicit operator FixedUtf8String(string s) => new FixedUtf8String(s);
        public static implicit operator string(FixedUtf8String utf8String) => utf8String.GetString();
    }

    public struct vkFixedArray2<T> where T : struct
    {
        public T First;
        public T Second;

        public vkFixedArray2(T first, T second)
        {
            First = first;
            Second = second;
        }

        public uint Count => 2;
    }

    public struct vkFixedArray3<T> where T : struct
    {
        public T First;
        public T Second;
        public T Third;

        public vkFixedArray3(T first, T second, T third)
        {
            First = first;
            Second = second;
            Third = third;
        }

        public uint Count => 3;
    }

    public struct vkFixedArray4<T> where T : struct
    {
        public T First;
        public T Second;
        public T Third;
        public T Fourth;

        public vkFixedArray4(T first, T second, T third, T fourth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
        }

        public uint Count => 4;
    }

    public struct vkFixedArray5<T> where T : struct
    {
        public T First;
        public T Second;
        public T Third;
        public T Fourth;
        public T Fifth;

        public vkFixedArray5(T first, T second, T third, T fourth, T fifth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
        }

        public uint Count => 5;
    }

    public struct vkFixedArray6<T> where T : struct
    {
        public T First;
        public T Second;
        public T Third;
        public T Fourth;
        public T Fifth;
        public T Sixth;

        public vkFixedArray6(T first, T second, T third, T fourth, T fifth, T sixth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Fifth = fifth;
            Sixth = sixth;
        }

        public uint Count => 6;
    }

    public static class vkFixedArray
    {
        public static vkFixedArray2<T> Create<T>(T first, T second) where T : struct
        {
            return new vkFixedArray2<T>(first, second);
        }

        public static vkFixedArray3<T> Create<T>(T first, T second, T third) where T : struct
        {
            return new vkFixedArray3<T>(first, second, third);
        }

        public static vkFixedArray4<T> Create<T>(T first, T second, T third, T fourth) where T : struct
        {
            return new vkFixedArray4<T>(first, second, third, fourth);
        }

        public static vkFixedArray5<T> Create<T>(T first, T second, T third, T fourth, T fifth) where T : struct
        {
            return new vkFixedArray5<T>(first, second, third, fourth, fifth);
        }

        public static vkFixedArray6<T> Create<T>(T first, T second, T third, T fourth, T fifth, T sixth) where T : struct
        {
            return new vkFixedArray6<T>(first, second, third, fourth, fifth, sixth);
        }
    }

    public class vkRawList<T> : IEnumerable<T>
    {
        private T[] _items;
        private uint _count;

        public const uint DefaultCapacity = 0;
        private const float GrowthFactor = 2f;

        public vkRawList() : this(DefaultCapacity) { }

        public vkRawList(uint capacity)
        {
            _items = capacity == 0 ? Array.Empty<T>() : new T[capacity];
            _count = (uint)_items.Length;
        }

        public uint Count
        {
            get => _count;
            set
            {
                Resize(value);
            }
        }


        public T[] Items => _items;

        public ArraySegment<T> ArraySegment => new ArraySegment<T>(_items, 0, (int)_count);

        public ref T this[uint index]
        {
            get
            {
                ValidateIndex(index);
                return ref _items[index];
            }
        }

        public ref T this[int index]
        {
            get
            {
                ValidateIndex(index);
                return ref _items[index];
            }
        }

        public void Add(ref T item)
        {
            if (_count == _items.Length)
            {
                if (_items.Length > 0)
                    Array.Resize(ref _items, (int)(_items.Length * GrowthFactor));
                else
                    Array.Resize(ref _items, (int)(1 * GrowthFactor));
            }

            _items[_count] = item;
            _count += 1;
        }

        public void Add(T item)
        {
            if (_count == _items.Length)
            {
                if (_items.Length > 0)
                    Array.Resize(ref _items, (int)(_items.Length * GrowthFactor));
                else
                    Array.Resize(ref _items, (int)(1 * GrowthFactor));
            }

            _items[_count] = item;
            _count += 1;
        }

        public void AddRange(T[] items)
        {
            var requiredSize = (int)(_count + items.Length);
            if (requiredSize > _items.Length)
            {
                Array.Resize(ref _items, (int)(requiredSize * GrowthFactor));
            }

            Array.Copy(items, 0, _items, (int)_count, items.Length);
            _count += (uint)items.Length;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void Replace(uint index, ref T item)
        {
            ValidateIndex(index);
            _items[index] = item;
        }

        public void Resize(uint count)
        {
            Array.Resize(ref _items, (int)count);
            _count = count;
        }

        public void Replace(uint index, T item) => Replace(index, ref item);

        public bool Remove(ref T item)
        {
            var contained = GetIndex(item, out var index);
            if (contained)
            {
                CoreRemoveAt(index);
            }

            return contained;
        }


        public bool Remove(T item)
        {
            var contained = GetIndex(item, out var index);
            if (contained)
            {
                CoreRemoveAt(index);
            }

            return contained;
        }

        public void RemoveAt(uint index)
        {
            ValidateIndex(index);
            CoreRemoveAt(index);
        }

        public void Clear()
        {
            Array.Clear(_items, 0, _items.Length);
        }

        public bool GetIndex(T item, out uint index)
        {
            var signedIndex = Array.IndexOf(_items, item);
            index = (uint)signedIndex;
            return signedIndex != -1;
        }

        public void Sort() => Sort(null);

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(_items, comparer);
        }

        public void TransformAll(Func<T, T> transformation)
        {
            for (var i = 0; i < _count; i++)
            {
                _items[i] = transformation(_items[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CoreRemoveAt(uint index)
        {
            _count -= 1;
            Array.Copy(_items, (int)index + 1, _items, (int)index, (int)(_count - index));
            _items[_count] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateIndex(uint index)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateIndex(int index)
        {
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private vkRawList<T> _list;
            private uint _currentIndex;

            public Enumerator(vkRawList<T> list)
            {
                _list = list;
                _currentIndex = uint.MaxValue;
            }

            public T Current => _list._items[_currentIndex];
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _currentIndex += 1;
                return _currentIndex < _list._count;
            }

            public void Reset()
            {
                _currentIndex = 0;
            }

            public void Dispose()
            {
            }
        }
    }
}
