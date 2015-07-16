using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using System.Drawing;

using SimpleScene.Util;

namespace SimpleScene
{
    public class SSParticle
    {
        public const int c_maxSupportedSpritePresets = 8;

        public float life = 1f;
        public Vector3 pos = new Vector3(0f);
        public Vector3 vel = new Vector3(1f);
		public Vector3 orientation = new Vector3(0f);
		public Vector3 angularVelocity = new Vector3 (0f);
        public float masterScale = 1f;
        public Vector3 componentScale = new Vector3(1f);
        public Color4 color = Color4.White;
        public float mass = 1.0f;
		public float rotationalInnertia = 1.0f;
		public float drag = 0.0f;
		public float rotationalDrag = 0.0f;
        public float viewDepth = float.PositiveInfinity;

        // when not -1 (255) means use sprite location preset as a source of UV for the current particle
		//public byte SpriteIndex = byte.MaxValue;

        // when not NaN means the values are used as a sorce of UV for the current particles
        public RectangleF spriteRect = new RectangleF (0f, 0f, 1f, 1f);
        // ^ if both indexed and custom uv values are specified they will be added in the shader
        // TODO orientation, effector mask

		public ushort effectorMask = ushort.MaxValue;

		public bool billboardXY {
			set { 
				if (value) { 
					orientation.X = orientation.Y = float.NaN;
				} else {
					if (float.IsNaN (orientation.X)) {
						orientation.X = 0f;
					}
					if (float.IsNaN (orientation.Y)) {
						orientation.Y = 0f;
					}
				}
			}
			get { return float.IsNaN (orientation.X) || float.IsNaN (orientation.Y); }
		}
    }

    /// <summary>
    /// Particle system simulates a number of particles.
    /// Particles are emitted by SSParticleEmitter's, which are responsible for assigning particle properties
    /// (inial position, velocity, color, etc.)
    /// Particles' position is updated during simulation to advance by their individual velocity
    /// For more advanced effects on particles (simulate gravity, fields, etc.) SSParticleEffector's are used
    /// </summary>

    public class SSParticleSystemData : SSInstancesData
	{
		protected static readonly SSAttributeVec3 _notAPosition = new SSAttributeVec3(new Vector3 (float.NaN));
		protected static Random _rand = new Random(); // for quicksorting

		/// <summary>
		/// Defines a unit step for the thic particle system's simulation. 
		/// Higher values result in a more quanitized simulation but more processing
		/// </summary>
        public float simulationStep = .025f;

        // TODO bounding sphere or cube
        protected List<SSParticleEmitter> _emitters = new List<SSParticleEmitter> ();
        protected List<SSParticleEffector> _effectors = new List<SSParticleEffector> ();

        protected readonly int _capacity;
        protected int _numParticles = 0;

        #region required particle data
        protected readonly float[] _lives;  // unused particles will be hacked to not draw
        protected int _nextIdxToWrite;      // change to UintPtr?
        protected int _nextIdxToOverwrite;
        protected int _activeBlockLength;
        #endregion

        #region particle data sent to the GPU
        protected SSAttributeVec3[] _positions;
		protected SSAttributeVec2[] _orientationsXY;
		protected SSAttributeFloat[] _orientationsZ;
        protected SSAttributeColor[] _colors;
        protected SSAttributeFloat[] _masterScales;
        protected SSAttributeVec2[] _componentScalesXY;
		protected SSAttributeFloat[] _componentScalesZ;

		//protected SSAttributeByte[] m_spriteIndices;
        protected SSAttributeFloat[] _spriteOffsetsU;
        protected SSAttributeFloat[] _spriteOffsetsV;
        protected SSAttributeFloat[] _spriteSizesU;
        protected SSAttributeFloat[] _spriteSizesV;
        #endregion

        #region particle data used for the simulation only
        protected Vector3[] _velocities;
        protected Vector3[] _angularVelocities;
        protected float[] _masses;
		protected float[] _rotationalInnertias;
		protected float[] _drags;
		protected float[] _rotationalDrags;
        protected float[] _viewDepths;
		protected byte[] _effectorMasksLow;
		protected byte[] _effectorMasksHigh;
        // TODO Orientation
        // TODO Texture Coord
        #endregion

        protected float _radius;
        protected float _timeDeltaAccumulator;

        public override int capacity { get { return _capacity; } }
		public override int activeBlockLength { get { return _activeBlockLength; } }
		public override float radius { get { return _radius; } }
		public override SSAttributeVec3[] positions { get { return _positions; } }
		public override SSAttributeVec2[] orientationsXY { get { return _orientationsXY; } }
		public override SSAttributeFloat[] orientationsZ { get { return _orientationsZ; } }
		public override SSAttributeColor[] colors { get { return _colors; } }
		public override SSAttributeFloat[] masterScales { get { return _masterScales; } }
		public override SSAttributeVec2[] componentScalesXY { get { return _componentScalesXY; } }
		public override SSAttributeFloat[] componentScalesZ { get { return _componentScalesZ; } }
		//public SSAttributeByte[] SpriteIndices { get { return m_spriteIndices; } }
		public override SSAttributeFloat[] spriteOffsetsU { get { return _spriteOffsetsU; ; } }
		public override SSAttributeFloat[] SpriteOffsetsV { get { return _spriteOffsetsV; } }
		public override SSAttributeFloat[] SpriteSizesU { get { return _spriteSizesU; } }
		public override SSAttributeFloat[] SpriteSizesV { get { return _spriteSizesV; } }

        public SSParticleSystemData (int capacity)
        {
            _capacity = capacity;
            _lives = new float[_capacity];
            reset();
        }

		/// <summary>
		/// This class tries faithfully to be resettable. One of the benefits may be being able to reset a
		/// particle system if working in a game editor.
		/// 
		/// Overriding this may be a good idea if you want dervied class to reset its member additions in 
		/// sync with the basic particle system reset. Should you chose to do so make sure to call
		/// this base function.
		/// </summary>
		public virtual void reset()
        {
            _nextIdxToWrite = 0;
            _nextIdxToOverwrite = 0;
            _activeBlockLength = 0;
			_numParticles = 0;

			_radius = 0f;
			_timeDeltaAccumulator = 0f;

            _positions = new SSAttributeVec3[1];
			_orientationsXY = new SSAttributeVec2[1];
			_orientationsZ = new SSAttributeFloat[1];
            _masterScales = new SSAttributeFloat[1];
            _componentScalesXY = new SSAttributeVec2[1];
			_componentScalesZ = new SSAttributeFloat[1];
            _colors = new SSAttributeColor[1];

			//m_spriteIndices = new SSAttributeByte[1];
            _spriteOffsetsU = new SSAttributeFloat[1];
            _spriteOffsetsV = new SSAttributeFloat[1];
            _spriteSizesU = new SSAttributeFloat[1];
            _spriteSizesV = new SSAttributeFloat[1];

            _velocities = new Vector3[1];
            _angularVelocities = new Vector3[1];
            _masses = new float[1];
			_rotationalInnertias = new float[1];
			_drags = new float[1];
			_rotationalDrags = new float[1];
            _viewDepths = new float[1];
			_effectorMasksLow = new byte[1];
			_effectorMasksHigh = new byte[1];

            writeParticle(0, new SSParticle ()); // fill in default values
            for (int i = 0; i < _capacity; ++i) {
                _lives [i] = 0f;
            }

            foreach (SSParticleEmitter emitter in _emitters) {
                emitter.reset();
            }
            foreach (SSParticleEffector effector in _effectors) {
                effector.reset();
            }
        }

		public virtual void emitAll()
        {
            foreach (SSParticleEmitter e in _emitters) {
                e.emitParticles(storeNewParticle);
            }
        }

		public override void update(float elapsedS)
        {
            elapsedS += _timeDeltaAccumulator;
            while (elapsedS >= simulationStep) {
                simulateStep();
                elapsedS -= simulationStep;
            }
            _timeDeltaAccumulator = elapsedS;
        }

		public virtual void addEmitter(SSParticleEmitter emitter)
        {
            emitter.reset();
            _emitters.Add(emitter);
        }

		public virtual void addEffector(SSParticleEffector effector)
        {
            effector.reset();
            _effectors.Add(effector);
        }

        public void sortByDepth(ref Matrix4 viewMatrix)
        {
            if (_numParticles == 0) return;

            for (int i = 0; i < _activeBlockLength; ++i) {
                if (isAlive(i)) {
                    // Do the transform and store z of the result
                    Vector3 pos = readData(_positions, i).Value;
                    pos = Vector3.Transform(pos, viewMatrix);
                    writeDataIfNeeded(ref _viewDepths, i, pos.Z);
                } else {
                    // since we are doing a sort pass later, might as well make it so
                    // so the dead particles get pushed the back of the arrays
                    writeDataIfNeeded(ref _viewDepths, i, float.PositiveInfinity);
                }
            }

            quickSort(0, _activeBlockLength - 1);

            if (_numParticles < _capacity) {
                // update pointers to reflect dead particles that just got sorted to the back
                _nextIdxToWrite = _numParticles - 1;
                _activeBlockLength = _numParticles;
            }

            #if false
            // set color based on the index of the particle in the arrays
            SSAttributeColor[] debugColors = {
                new SSAttributeColor(OpenTKHelper.Color4toRgba(Color4.Red)),
                new SSAttributeColor(OpenTKHelper.Color4toRgba(Color4.Green)),
                new SSAttributeColor(OpenTKHelper.Color4toRgba(Color4.Blue)),
                new SSAttributeColor(OpenTKHelper.Color4toRgba(Color4.Yellow))
            };

            for (int i = 0; i < m_activeBlockLength; ++i) {
                writeDataIfNeeded(ref m_colors, i, debugColors[i]);
            }
            #endif
        }

		protected virtual void simulateStep()
        {
            _radius = 0f;
			foreach (SSParticleEffector effector in _effectors) {
				effector.simulateSelf (simulationStep);
			}
			foreach (SSParticleEmitter emitter in _emitters) {
				emitter.simulateSelf (simulationStep);
			}

            SSParticle p = new SSParticle ();
            for (int i = 0; i < _activeBlockLength; ++i) {
                if (isAlive(i)) {
                    // Alive particle
                    _lives [i] -= simulationStep;
                    if (isAlive(i)) {
                        // Still alive. Update position and run through effectors
                        readParticle(i, p);
						p.vel -= p.drag * p.vel / p.mass;
						if (!p.billboardXY) {
							p.angularVelocity.Xy -= p.rotationalDrag * p.angularVelocity.Xy / p.rotationalInnertia;
						}
						p.angularVelocity.Z -= p.rotationalDrag * p.angularVelocity.Z / p.rotationalInnertia;
                        p.pos += p.vel * simulationStep;
						if (!p.billboardXY) {
							p.orientation.Xy += p.angularVelocity.Xy * simulationStep;
						}
						p.orientation.Z += p.angularVelocity.Z * simulationStep;
                        foreach (SSParticleEffector effector in _effectors) {
							effector.simulateParticleEffect(p, simulationStep);
                        }
                        writeParticle(i, p);
                        float distFromOrogin = p.pos.Length;
                        if (distFromOrogin > _radius) {
                            _radius = distFromOrogin;
                        }
                    } else {
                        // Particle just died. Hack to not draw?
                        writeDataIfNeeded(ref _positions, i, _notAPosition);
                        if (_numParticles == _capacity || i < _nextIdxToWrite) {
                            // released slot will be the next one to be written to
                            _nextIdxToWrite = i;
                        }
                        if (i == _activeBlockLength - 1) {
                            // reduction in the active particles block
                            while (_activeBlockLength > 0 && isDead(_activeBlockLength - 1)) {
                                --_activeBlockLength;
                            }
                        }                        
						--_numParticles;
                        if (_numParticles == 0) {
                            // all particles gone. reset write and overwrite locations for better packing
                            _nextIdxToWrite = 0;
                            _nextIdxToOverwrite = 0;
                            _activeBlockLength = 0;
                        }
                    }
                }
            }
			foreach (SSParticleEmitter emitter in _emitters) {
				emitter.simulateEmissions(simulationStep, storeNewParticle);
			}
        }

        protected virtual void storeNewParticle(SSParticle newParticle)
        {
			// Apply effects before storing the new particle
			// This can help avoid unnecessary array expansion for new particle's values
			foreach (SSParticleEffector effector in _effectors) {
				effector.simulateParticleEffect(newParticle, simulationStep);
			}

            int writeIdx;
            if (_numParticles == _capacity) {
                writeIdx = _nextIdxToOverwrite;
                _nextIdxToOverwrite = nextIdx(_nextIdxToOverwrite);
            } else {
                while (isAlive(_nextIdxToWrite)) {
                    _nextIdxToWrite = nextIdx(_nextIdxToWrite);
                }
                writeIdx = _nextIdxToWrite;
                if (writeIdx + 1 >= _activeBlockLength) {
                    _activeBlockLength = writeIdx + 1;
                }
                _nextIdxToWrite = nextIdx(_nextIdxToWrite);
                _numParticles++;
            }
            writeParticle(writeIdx, newParticle);

			float distFromOrogin = newParticle.pos.Length;
			if (distFromOrogin > _radius) {
				_radius = distFromOrogin;
			}
        }

        protected int nextIdx(int idx) 
        {
            ++idx;
            if (idx == _capacity) {
                idx = 0;
            }
            return idx;
        }

        protected bool isDead(int idx) {
            return _lives [idx] <= 0f;
        }

        protected bool isAlive(int idx) {
            return !isDead(idx);
        }

        protected T readData<T>(T[] array, int idx)
        {
            if (idx >= array.Length) {
                return array [0];
            } else {
                return array [idx];
            }
        }

        protected virtual void readParticle (int idx, SSParticle p) {
            p.life = _lives [idx];
            p.pos = readData(_positions, idx).Value;
			p.orientation.Xy = readData(_orientationsXY, idx).Value;
			p.orientation.Z = readData (_orientationsZ, idx).Value;
            p.viewDepth = readData(_viewDepths, idx);

            if (p.life <= 0f) return; // the rest does not matter

            p.masterScale = readData(_masterScales, idx).Value;
			p.componentScale.Xy = readData(_componentScalesXY, idx).Value;
			p.componentScale.Z = readData (_componentScalesZ, idx).Value;
			p.color = Color4Helper.FromUInt32(readData(_colors, idx).Color);

			//p.SpriteIndex = readData(m_spriteIndices, idx).Value;
            p.spriteRect.X = readData(_spriteOffsetsU, idx).Value;
            p.spriteRect.Y = readData(_spriteOffsetsV, idx).Value;
            p.spriteRect.Width = readData(_spriteSizesU, idx).Value;
            p.spriteRect.Height = readData(_spriteSizesV, idx).Value;

            p.vel = readData(_velocities, idx);
            p.angularVelocity = readData(_angularVelocities, idx);
            p.mass = readData(_masses, idx);
			p.rotationalInnertia = readData (_rotationalInnertias, idx);
			p.drag = readData (_drags, idx);
			p.rotationalDrag = readData (_rotationalDrags, idx);
			p.effectorMask = (ushort)((int)readData (_effectorMasksHigh, idx) << 8
							 	    | (int)readData (_effectorMasksLow, idx));
        }

        protected void writeDataIfNeeded<T>(ref T[] array, int idx, T value) where T : IEquatable<T>
        {
            bool write = true;
			if (_activeBlockLength > 1 && array.Length == 1) {
                T masterVal = array [0];
                if (masterVal.Equals(value)) {
                    write = false;
                } else {
                    // allocate the array to keep track of different values
                    array = new T[_capacity];
                    for (int i = 0; i < _activeBlockLength; ++i) {
                        array [i] = masterVal;
                    }
                }
            }
            if (write) {
                array [idx] = value;
            }
        }

        protected virtual void writeParticle(int idx, SSParticle p) {
            _lives [idx] = p.life;
			writeDataIfNeeded(ref _positions, idx, new SSAttributeVec3(p.pos));
			writeDataIfNeeded(ref _orientationsXY, idx, new SSAttributeVec2 (p.orientation.Xy));
			writeDataIfNeeded(ref _orientationsZ, idx, new SSAttributeFloat (p.orientation.Z));
			writeDataIfNeeded(ref _masterScales, idx, new SSAttributeFloat (p.masterScale));
			writeDataIfNeeded(ref _componentScalesXY, idx, new SSAttributeVec2 (p.componentScale.Xy));
			writeDataIfNeeded(ref _componentScalesZ, idx, new SSAttributeFloat (p.componentScale.Z));
			writeDataIfNeeded(ref _colors, idx, new SSAttributeColor(Color4Helper.ToUInt32(p.color)));

			//writeDataIfNeeded(ref m_spriteIndices, idx, new SSAttributeByte(p.SpriteIndex));
            writeDataIfNeeded(ref _spriteOffsetsU, idx, new SSAttributeFloat(p.spriteRect.X));
            writeDataIfNeeded(ref _spriteOffsetsV, idx, new SSAttributeFloat(p.spriteRect.Y));
            writeDataIfNeeded(ref _spriteSizesU, idx, new SSAttributeFloat(p.spriteRect.Width));
            writeDataIfNeeded(ref _spriteSizesV, idx, new SSAttributeFloat(p.spriteRect.Height));

            writeDataIfNeeded(ref _velocities, idx, p.vel);
            writeDataIfNeeded(ref _angularVelocities, idx, p.angularVelocity);
            writeDataIfNeeded(ref _masses, idx, p.mass);
			writeDataIfNeeded(ref _rotationalInnertias, idx, p.rotationalInnertia);
			writeDataIfNeeded(ref _drags, idx, p.drag);
			writeDataIfNeeded(ref _rotationalDrags, idx, p.rotationalDrag);
            writeDataIfNeeded(ref _viewDepths, idx, p.viewDepth);
			writeDataIfNeeded(ref _effectorMasksHigh, idx, (byte)((p.effectorMask & 0xFF00) >> 8));
			writeDataIfNeeded(ref _effectorMasksLow, idx, (byte)(p.effectorMask & 0xFF));
        }

        // The alternative to re-implementing quicksort appears to be implementing IList interface with
        // about 10 function implementations needed, some of which may be messy or not make sense for the
        // current data structure. For now lets implement quicksort and see how it does
        protected void quickSort(int leftIdx, int rightIdx)
        {
            if (leftIdx < rightIdx) {
                int pi = quicksortPartition(leftIdx, rightIdx);
                quickSort(leftIdx, pi - 1);
                quickSort(pi + 1, rightIdx);
            }   
        }

        protected int quicksortPartition(int leftIdx, int rightIdx)
        {
            int pivot = _rand.Next(leftIdx, rightIdx);
            particleSwap(pivot, rightIdx);
            int store = leftIdx;

            for (int i = leftIdx; i < rightIdx; ++i) {
                float iDepth = readData(_viewDepths, i);
                float rightDepth = readData(_viewDepths, rightIdx);
                // or <= ?
                if (iDepth <= rightDepth) {
                    particleSwap(i, store);
                    store++;
                }
            }
            particleSwap(store, rightIdx);
            return store;
        }

        protected void particleSwap(int leftIdx, int rightIdx)
        {
            // TODO Consider swaping on a per component basis. 
            // It may have better peformance
            // But adds more per-component maintenance
            SSParticle leftParticle = new SSParticle ();
            SSParticle rightParticle = new SSParticle();
            readParticle(leftIdx, leftParticle);
            readParticle(rightIdx, rightParticle);

            // write in reverse
            writeParticle(leftIdx, rightParticle);
            writeParticle(rightIdx, leftParticle);
        }
    }
}

