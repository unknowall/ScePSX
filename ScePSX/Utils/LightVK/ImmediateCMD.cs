
using System;
using System.Collections.Generic;
using static LightVK.VulkanNative;

namespace LightVK
{
    //时间线信号量必须显卡支持 1.2+
    public unsafe class ImmediateCMD : IDisposable
    {
        VulkanDevice Device;

        readonly static int SemaphoreTypeCreateInfo = 1000207002; // VK_STRUCTURE_TYPE_SEMAPHORE_TYPE_CREATE_INFO
        readonly static int TimelineSemaphoreSubmitInfo = 1000207003; // VK_STRUCTURE_TYPE_TIMELINE_SEMAPHORE_SUBMIT_INFO
        readonly static int SemaphoreWaitInfo = 1000207004; // VK_STRUCTURE_TYPE_SEMAPHORE_WAIT_INFO

        private const int RING_SIZE = 3;
        private readonly VkCommandBuffer[] _commandRings = new VkCommandBuffer[RING_SIZE];
        private readonly VkFence[] _fences = new VkFence[RING_SIZE];
        private int _currentRingIndex;

        // 时间轴信号量（Vulkan 1.2+）
        private VkSemaphore _timelineSemaphore;
        private ulong _timelineValue = 0;

        private VkCommandPool cmdPool;

        private readonly Stack<VkCommandBuffer> _emergencyBuffers = new();

        public unsafe ImmediateCMD(VulkanDevice device)
        {
            Device = device;

            cmdPool = Device.CreateCommandPool();

            for (int i = 0; i < RING_SIZE; i++)
            {
                _commandRings[i] = CreateTransientCommandBuffer();
                _fences[i] = Device.CreateFence(true);
            }

            // 创建时间轴信号量
            var st = new VkSemaphoreTypeCreateInfo
            {
                sType = (VkStructureType)SemaphoreTypeCreateInfo,
                semaphoreType = VkSemaphoreType.VK_SEMAPHORE_TYPE_TIMELINE,
                initialValue = 0
            };
            var semaphoreCreateInfo = new VkSemaphoreCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO,
                pNext = &st
            };

            fixed (VkSemaphore* timelineSemaphorePtr = &_timelineSemaphore)
                vkCreateSemaphore(Device.device, &semaphoreCreateInfo, null, timelineSemaphorePtr);
        }

        public void Dispose()
        {
            vkDestroyCommandPool(Device.device, cmdPool, null);

            vkDestroySemaphore(Device.device, _timelineSemaphore, null);
        }

        public VkCommandBuffer GetImmediateCommandBuffer()
        {
            if (vkGetFenceStatus(Device.device, _fences[_currentRingIndex]) == VkResult.VK_SUCCESS)
            {
                var cmd = _commandRings[_currentRingIndex];
                vkResetCommandBuffer(cmd, VkCommandBufferResetFlags.None);
                return cmd;
            }

            if (_emergencyBuffers.TryPop(out var emergencyCmd))
            {
                vkResetCommandBuffer(emergencyCmd, VkCommandBufferResetFlags.None);
                return emergencyCmd;
            }

            var newCmd = CreateTransientCommandBuffer();
            _emergencyBuffers.Push(newCmd);
            return newCmd;
        }

        public void SubmitImmediate(VkCommandBuffer cmd)
        {
            ulong ssv = ++_timelineValue;

            var signalInfo = new VkTimelineSemaphoreSubmitInfo
            {
                sType = (VkStructureType)TimelineSemaphoreSubmitInfo,
                signalSemaphoreValueCount = 1,
                pSignalSemaphoreValues = &ssv
            };

            var ts = _timelineSemaphore;

            var submitInfo = new VkSubmitInfo
            {
                pNext = &signalInfo,
                sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                commandBufferCount = 1,
                pCommandBuffers = &cmd,
                signalSemaphoreCount = 1,
                pSignalSemaphores = &ts
            };

            // 异步提交（不带围栏）
            vkQueueSubmit(Device.graphicsQueue, 1, &submitInfo, VkFence.Null);

            if (IsRingBuffer(cmd))
            {
                fixed (VkFence* fencePtr = &_fences[_currentRingIndex])
                    vkResetFences(Device.device, 1, fencePtr);
                _currentRingIndex = (_currentRingIndex + 1) % RING_SIZE;
            }
        }

        public unsafe void FinishSemaphore()
        {
            var ts = _timelineSemaphore;
            var tv = _timelineValue;
            var waitInfo = new VkSemaphoreWaitInfo
            {
                sType = (VkStructureType)SemaphoreWaitInfo,
                semaphoreCount = 1,
                pSemaphores = &ts,
                pValues = &tv
            };

            if (vkWaitSemaphores(Device.device, &waitInfo, 0) == VkResult.VK_SUCCESS)
            {
                while (_emergencyBuffers.Count > 0)
                {
                    var cmd = _emergencyBuffers.Pop();
                    vkResetCommandBuffer(cmd, VkCommandBufferResetFlags.None);
                    if (!IsRingBuffer(cmd))
                    {
                        vkFreeCommandBuffers(Device.device, cmdPool, 1, &cmd);
                    }
                }
            }
        }

        private bool IsRingBuffer(VkCommandBuffer cmd)
        {
            foreach (var ringCmd in _commandRings)
            {
                if (ringCmd.Handle == cmd.Handle)
                    return true;
            }
            return false;
        }

        private VkCommandBuffer CreateTransientCommandBuffer()
        {
            var allocInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
                commandPool = cmdPool,
                level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY,
                commandBufferCount = 1
            };
            var cmd = VkCommandBuffer.Null;
            vkAllocateCommandBuffers(Device.device, &allocInfo, &cmd);
            return cmd;
        }
    }

}
