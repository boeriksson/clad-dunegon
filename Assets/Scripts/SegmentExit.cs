using LocalDirection = Direction.LocalDirection;
using GlobalDirection = Direction.GlobalDirection;
using DirectionConversion = Direction.DirectionConversion;
using Debug = UnityEngine.Debug;

namespace Segment {
    public class SegmentExit {
        private int _x;
        private int _z;
        private GlobalDirection _direction;

        public SegmentExit(int entryX, int entryZ, GlobalDirection gDirection, int forward, int right, LocalDirection lDirection) {
            _direction = DirectionConversion.GetDirection(gDirection, lDirection);
            //Debug.Log("SegmentExit gDirection: " + gDirection + " localDirection: " + lDirection + " _direction: " + _direction);
            switch (gDirection) {
                case GlobalDirection.North: {
                    _x = entryX + forward;
                    _z = entryZ + right;
                    break;
                }
                case GlobalDirection.East: {
                    _x = entryX - right;
                    _z = entryZ + forward;
                    break;
                }
                case GlobalDirection.South: {
                    _x = entryX - forward;
                    _z = entryZ - right;
                    break;
                }
                case GlobalDirection.West: {
                    _x = entryX + right;
                    _z = entryZ - forward;
                    break;
                }
            }
        }

        public int X {
            get {
                return _x;
            }
        }

        public int Z {
            get {
                return _z;
            }
        }

        public GlobalDirection Direction {
            get {
                return _direction;
            }
        }

        public override string ToString(){
            return "{" + _x + ", " + _z + "} Gdirection: " + _direction;
        } 
    }
}
