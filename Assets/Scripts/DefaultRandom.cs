using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace util {
    public interface RandomGenerator {
        int Generate(int totalWeight);
    }
    public class DefaultRandom : RandomGenerator {
        public int Generate(int totalWeight) {
            return Random.Range(1, totalWeight);
        }
    } 
    public class MockedRandom : RandomGenerator {
        private int randomResult;
        public MockedRandom(int _randomResult) {
            randomResult = _randomResult;
        }
        public int Generate(int totalWeight) {
            return randomResult;
        }
    }
}
