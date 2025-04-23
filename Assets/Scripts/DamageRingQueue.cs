using Unity.Mathematics;
using UnityEngine;

namespace ST.HUD
{
    public struct FloatingDamage
    {
        public uint3x3 uvVexIdx;
        public float2 scale;
        public float3 wpos;
        public float fixedTime;
    }

    public class DamageRingQueue
    {
        private static DamageRingQueue instance = default;
        public static DamageRingQueue Instance => instance;

        private readonly FloatingDamage[] _buffer;
        private int _head;
        private int _tail;
        private int _transfer;
        private readonly int _capacity;
        
        public int Count => (_tail - _head + _capacity) % _capacity;

        public DamageRingQueue(int capacity)
        {
            _capacity = capacity;
            _buffer = new FloatingDamage[_capacity];
            _head = 0;
            _tail = 0;
            _transfer = 0;
            instance = this;
        }
        
        public void Enqueue(FloatingDamage item)
        {
            _buffer[_tail] = item;
            _tail = (_tail + 1) % _capacity;
            if (_tail == _head)
                _head = (_head + 1) % _capacity;
        }
        
        public void TryAppendData(ComputeBuffer buffer)
        {
            if (_tail > _transfer)
            {
                buffer.SetData(_buffer, _transfer, _transfer, _tail - _transfer);
                _transfer = _tail;
            }
            else if (_tail < _transfer)
            {
                buffer.SetData(_buffer, _transfer, _transfer, _capacity - _transfer);
                buffer.SetData(_buffer, 0, 0, _tail);
                _transfer = _tail;
            }
        }
    }

}