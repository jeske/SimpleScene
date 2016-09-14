using System;
using System.Drawing;
using SimpleScene.Util;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene
{
    /// <summary>
    /// Emits particles on demand via EmitParticles(...) or periodically via Simulate(...)
    /// </summary>
    public abstract class SSParticleEmitter
    {
        public delegate SSParticle ParticleFactory();
        public delegate int ReceiverHandler(SSParticle newParticle);

        protected readonly static SSParticle _defaultParticle = new SSParticle();

        protected static Random _rand = new Random ();

        public float emissionDelay = 0f;

        public float emissionIntervalMin = 1.0f;
        public float emissionIntervalMax = 1.0f;
        public float emissionInterval {
            set { emissionIntervalMin = emissionIntervalMax = value; }
        }

        public int particlesPerEmissionMin = 1;
        public int particlesPerEmissionMax = 1;
        public int particlesPerEmission {
            set { particlesPerEmissionMin = particlesPerEmissionMax = value; }
        }

		/// <summary>
		/// When >= 0 indices emissions left. When -1 means emit infinitely (default)
		/// </summary>
		public int totalEmissionsLeft = -1;

        public float lifeMin = _defaultParticle.life;
        public float lifeMax = _defaultParticle.life;
        public float life {
            set { lifeMin = lifeMax = value; }
        }

        public Vector3 velocityComponentMin = _defaultParticle.vel;
        public Vector3 velocityComponentMax = _defaultParticle.vel;
        public Vector3 velocity {
            set { velocityComponentMin = velocityComponentMax = value; }
        }

        private Vector3 _orientationMin = _defaultParticle.orientation;
        private Vector3 _orientationMax = _defaultParticle.orientation;

		public Vector3 orientationMin {
			get { return _orientationMin; }
			set {
				if (!billboardXY) {
					if (!float.IsNaN (value.X)) {
						_orientationMin.X = value.X;
					}
					if (!float.IsNaN (value.Y)) {
						_orientationMin.Y = value.Y;
					}
				}
				_orientationMin.Z = value.Z;
			}
		}
		public Vector3 orientationMax {
			get { return _orientationMax; }
			set {
				if (!billboardXY) {
					if (!float.IsNaN (value.X)) {
						_orientationMax.X = value.X;
					}
					if (!float.IsNaN (value.Y)) {
						_orientationMax.Y = value.Y;
					}
				}
				_orientationMax.Z = value.Z;
			}
		}
        public Vector3 orientation {
            set { 
				_orientationMin = _orientationMax = value; 
				if (billboardXY) {
					billboardXY = true; // maintain BillboardXY state
				}
			}
        }

		public bool billboardXY {
			set { 
				if (value) { 
					_orientationMin.X = _orientationMax.X = _orientationMin.Y = _orientationMax.Y = float.NaN;
				} else {
					if (float.IsNaN (_orientationMin.X)) {
						_orientationMin.X = 0f;
					}
					if (float.IsNaN (_orientationMax.X)) {
						_orientationMax.X = 0f;
					}
					if (float.IsNaN (_orientationMin.Y)) {
						_orientationMin.Y = 0f;
					}
					if (float.IsNaN (_orientationMax.Y)) {
						_orientationMax.Y = 0f;
					}
				}
			}
			get { 			
				return float.IsNaN (_orientationMin.X) || float.IsNaN (_orientationMax.X)
					|| float.IsNaN (_orientationMin.Y) || float.IsNaN (_orientationMax.Y);
			}
		}

        public Vector3 angularVelocityMin = _defaultParticle.angularVelocity;
        public Vector3 angularVelocityMax = _defaultParticle.angularVelocity;
        public Vector3 angularVelocity {
            set { angularVelocityMin = angularVelocityMax = value; }
        }

        public float masterScaleMin = _defaultParticle.masterScale;
        public float masterScaleMax = _defaultParticle.masterScale;
        public float masterScale {
            set { masterScaleMin = masterScaleMax = value; }
        }

        public Vector3 componentScaleMin = _defaultParticle.componentScale;
        public Vector3 componentScaleMax = _defaultParticle.componentScale;
        public Vector3 componentScale {
            set { componentScaleMin = componentScaleMax = value; }
        }

		#region final color = random color preset + random component offset
		/// <summary>
		/// Just set all particles to be this color! (Simplify color picking) 
		/// </summary>
		/// <value>The color.</value>
		public Color4 color {
			set { 
				colorOffsetComponentMin = colorOffsetComponentMax = Color4Helper.Zero;
				if (colorPresets == null || colorPresets.Length != 1) {
					// avoid reallocation if used frequently
					colorPresets = new Color4[1];
				}
				colorPresets [0] = value;
			}
		}

		/// <summary>
		/// Color presets. Values selected at random when picking a color before being added to a random
		/// offset to form the final value
		/// </summary>
		public Color4[] colorPresets = { new Color4(1f, 1f, 1f, 1f) };

		/// <summary>
		/// Minimum value, split into components, for a color offset that gets added to preset to
		/// form the final color value
		/// </summary>
		public Color4 colorOffsetComponentMin = Color4Helper.Zero;
		/// <summary>
		/// Maximum value, split into components, for a color offset that gets added to preset to
		/// form the final color value
		/// </summary>
		public Color4 colorOffsetComponentMax = Color4Helper.Zero;
		#endregion

		public float massMin = _defaultParticle.mass;
		public float massMax = _defaultParticle.mass;
		public float mass {
			set { massMin = massMax = value; }
		}

		public float rotationalInnertiaMin = _defaultParticle.rotationalInnertia;
		public float rotationalInnertiaMax = _defaultParticle.rotationalInnertia;
		public float rotationalInnertia {
			set { rotationalInnertiaMin = rotationalInnertiaMax = value; }
		}

		public float dragMin = _defaultParticle.drag;
		public float dragMax = _defaultParticle.drag;
		public float drag {
			set { dragMin = dragMax = value; }
		}

		public float rotationalDragMin = _defaultParticle.rotationalDrag;
		public float rotationalDragMax = _defaultParticle.rotationalDrag;
		public float rotationalDrag {
			set { rotationalDragMin = rotationalDragMax = value; }
		}

        public RectangleF[] spriteRectangles = { _defaultParticle.spriteRect };
		//public byte[] SpriteIndices = { c_defaultParticle.SpriteIndex };

		public ushort[] effectorMasks = { _defaultParticle.effectorMask };
		public ushort effectorMask {
			set { 
				effectorMasks = new ushort[1]; 
				effectorMasks [0] = value;
			}
		}

        private float _initialDelay;
        private float _timeSinceLastEmission;
        private float _nextEmission;

		public SSParticleEmitter()
		{
			reset ();
		}

        public virtual void reset()
        {
            _initialDelay = emissionDelay;
            _timeSinceLastEmission = 0f;
            _nextEmission = 0f;
        }

        public void emitParticles(ParticleFactory factory, ReceiverHandler receiver)
        {
            int numToEmit = _rand.Next(particlesPerEmissionMin, particlesPerEmissionMax);
            emitParticles(numToEmit, factory, receiver);
        }

        public void simulateEmissions(float deltaT, ParticleFactory factory, ReceiverHandler receiver) 
        {
			if (totalEmissionsLeft == 0) return;

            if (_initialDelay > 0f) {
                // if initial delay is needed
                _initialDelay -= deltaT;
                if (_initialDelay > 0f) {
                    return;
                }
            }

            _timeSinceLastEmission += deltaT;
			float diff;
			while ((diff = _timeSinceLastEmission - _nextEmission) > 0f) {
                emitParticles(factory, receiver);
				if (totalEmissionsLeft > 0 && --totalEmissionsLeft == 0) {
					reset ();
                    break;
				} else {
                    _timeSinceLastEmission = diff;
					_nextEmission = Interpolate.Lerp(emissionIntervalMin, emissionIntervalMax, 
						(float)_rand.NextDouble());
				}
            }
        }

		public virtual void simulateSelf(float deltaT) { }

        /// <summary>
        /// Convenience function.
        /// </summary>
        static protected float nextFloat()
        {
            return (float)_rand.NextDouble();
        }

        /// <summary>
        /// Override by the derived classes to describe how new particles are emitted
        /// </summary>
        /// <param name="particleCount">Particle count.</param>
        /// <param name="receiver">Receiver.</param>
        protected virtual void emitParticles (int particleCount, ParticleFactory factory, ReceiverHandler receiver)
		{
            SSParticle newParticle = factory();
			for (int i = 0; i < particleCount; ++i) {
				configureNewParticle (newParticle);
				receiver (newParticle);
			}
		}

        /// <summary>
        /// To be used by derived classes for shared particle setup
        /// </summary>
        /// <param name="p">particle to setup</param>
        protected virtual void configureNewParticle(SSParticle p)
        {
            p.life = Interpolate.Lerp(lifeMin, lifeMax, nextFloat());

            p.componentScale.X = Interpolate.Lerp(componentScaleMin.X, componentScaleMax.X, nextFloat());
            p.componentScale.Y = Interpolate.Lerp(componentScaleMin.Y, componentScaleMax.Y, nextFloat());
            p.componentScale.Z = Interpolate.Lerp(componentScaleMin.Z, componentScaleMax.Z, nextFloat());

			if (billboardXY) {
				p.billboardXY = true;
			} else {
				p.orientation.X = Interpolate.Lerp (_orientationMin.X, _orientationMax.X, nextFloat ());
				p.orientation.Y = Interpolate.Lerp (_orientationMin.Y, _orientationMax.Y, nextFloat ());
			}
            p.orientation.Z = Interpolate.Lerp(_orientationMin.Z, _orientationMax.Z, nextFloat());

            p.angularVelocity.X = Interpolate.Lerp(angularVelocityMin.X, angularVelocityMax.X, nextFloat());
            p.angularVelocity.Y = Interpolate.Lerp(angularVelocityMin.Y, angularVelocityMax.Y, nextFloat());
            p.angularVelocity.Z = Interpolate.Lerp(angularVelocityMin.Z, angularVelocityMax.Z, nextFloat());

            p.vel.X = Interpolate.Lerp(velocityComponentMin.X, velocityComponentMax.X, nextFloat());
            p.vel.Y = Interpolate.Lerp(velocityComponentMin.Y, velocityComponentMax.Y, nextFloat());
            p.vel.Z = Interpolate.Lerp(velocityComponentMin.Z, velocityComponentMax.Z, nextFloat());

            p.masterScale = Interpolate.Lerp(masterScaleMin, masterScaleMax, nextFloat());

			p.mass = Interpolate.Lerp (massMin, massMax, nextFloat ());
			p.rotationalInnertia = Interpolate.Lerp (rotationalInnertiaMin, rotationalInnertiaMax, nextFloat ());
			p.drag = Interpolate.Lerp (dragMin, dragMax, nextFloat ());
			p.rotationalDrag = Interpolate.Lerp (rotationalDragMin, rotationalDragMax, nextFloat ());

			// color presets
			Color4 randPreset;
			if (colorPresets != null && colorPresets.Length > 0) {
				randPreset = colorPresets [_rand.Next (0, colorPresets.Length)];
			} else {
				randPreset = new Color4(0f, 0f, 0f, 0f);
			}

			// color offsets
			Color4 randOffset;
			randOffset.R = Interpolate.Lerp(colorOffsetComponentMin.R, colorOffsetComponentMax.R, nextFloat());
			randOffset.G = Interpolate.Lerp(colorOffsetComponentMin.G, colorOffsetComponentMax.G, nextFloat());
			randOffset.B = Interpolate.Lerp(colorOffsetComponentMin.B, colorOffsetComponentMax.B, nextFloat());
			randOffset.A = Interpolate.Lerp(colorOffsetComponentMin.A, colorOffsetComponentMax.A, nextFloat());

			// color presets + offsets
			p.color = Color4Helper.Add(ref randPreset, ref randOffset);

			//p.SpriteIndex = SpriteIndices [s_rand.Next(0, SpriteIndices.Length)];
            p.spriteRect = spriteRectangles [_rand.Next(0, spriteRectangles.Length)];

			p.effectorMask = effectorMasks [_rand.Next (0, effectorMasks.Length)];
        }
    }

	/// <summary>
	/// Emits particles at the origin
	/// </summary>
	public class SSFixedPositionEmitter : SSParticleEmitter
	{
		public Vector3 position;

		public SSFixedPositionEmitter (Vector3 _position)
		{
			position = _position;
		}

		public SSFixedPositionEmitter()
		{
			position = Vector3.Zero;
		}

		protected override void configureNewParticle (SSParticle p)
		{
			base.configureNewParticle (p);
			p.pos = position;
		}
	}

	/// <summary>
	/// Emitter that sends things moving away from the center in different directions 
	/// </summary>
	public class SSRadialEmitter : SSParticleEmitter
	{
		public Vector3 center = Vector3.Zero;

        /// <summary>
        /// Axis around which theta angles are measured. MUST be normalized
        /// </summary>
        public Vector3 up = Vector3.UnitZ;

		#region spawn radius
		public float radiusOffsetMin = 0f;
		public float radiusOffsetMax = 0f;
		public float radiusOffset {
			set { radiusOffsetMin = radiusOffsetMax = value; }
		}
		#endregion

		#region theta of the spawn and velocity
		public float thetaMin = 0f;
		public float thetaMax = 2f * (float)Math.PI;
		public float theta {
			set { thetaMin = thetaMax = value; }
		}
		#endregion

		#region phi of the spawn and velocity
		public float phiMin = -0.5f * (float)Math.PI;
		public float phiMax = +0.5f * (float)Math.PI;
		public float phi {
			set { phiMin = phiMax = value; }
		}
		#endregion

		#region velocity 
		public float velocityFromCenterMagnitudeMin = 1f;
		public float velocityFromCenterMagnitudeMax = 1f;
		public float velocityFromCenterMagnitude {
			set { velocityFromCenterMagnitudeMin = velocityFromCenterMagnitudeMax = value; }
		}
		#endregion

		#region special behaviors
		public bool orientAwayFromCenter = false;
		#endregion

        public SSRadialEmitter()
        {
            // non-radial velocity offset zero by default
            base.velocity = Vector3.Zero;
        }

		protected override void configureNewParticle (SSParticle p)
		{
			base.configureNewParticle (p);
			float r = Interpolate.Lerp (radiusOffsetMin, radiusOffsetMax, nextFloat());
			float theta = Interpolate.Lerp (thetaMin, thetaMax, nextFloat ());
			float phi = Interpolate.Lerp (phiMin, phiMax, nextFloat ());
			float xy = (float)Math.Cos (phi);
			float x = xy * (float)Math.Cos (theta);
			float y = xy * (float)Math.Sin (theta);
			float z = (float)Math.Sin (phi);

            Vector3 xAxis, yAxis;
            OpenTKHelper.TwoPerpAxes(up, out xAxis, out yAxis);
            Vector3 xyz = x * xAxis + y * yAxis + z * up;

			p.pos = center + r * xyz;
			float velocityMag = Interpolate.Lerp (velocityFromCenterMagnitudeMin, velocityFromCenterMagnitudeMax, nextFloat ());
			p.vel += velocityMag * xyz;

			if (orientAwayFromCenter) {
				p.orientation.Z = -theta;
				p.orientation.Y = phi;
			}
            //Console.WriteLine("particle emitted with vel = " + p.vel.Length);
		}
	}

    /// <summary>
    /// Emits via an instance of ParticlesFieldGenerator
    /// </summary>
    public class SSParticlesFieldEmitter : SSParticleEmitter
    {
        protected ParticlesFieldGenerator m_fieldGenerator;

		public ParticlesFieldGenerator Field  {
			get { return m_fieldGenerator; }
		}

        public SSParticlesFieldEmitter(ParticlesFieldGenerator fieldGenerator)
        {
            m_fieldGenerator = fieldGenerator;
        }

        public void setSeed(int seed)
        {
            m_fieldGenerator.SetSeed(seed);
        }

        protected override void emitParticles (int particleCount, ParticleFactory factory, ReceiverHandler particleReceiver)
        {
            SSParticle newParticle = factory();
            ParticlesFieldGenerator.NewParticleDelegate fieldReceiver = (id, pos) => {
                configureNewParticle(newParticle);
                newParticle.pos = pos;
                particleReceiver(newParticle);
                return true;
            };
            m_fieldGenerator.Generate(particleCount, fieldReceiver);
        }
    }

	public class SSBodiesFieldEmitter : SSParticleEmitter
	{
		protected BodiesFieldGenerator _bodiesGenerator;

		public SSBodiesFieldEmitter(BodiesFieldGenerator fieldGenerator)
		{
			_bodiesGenerator = fieldGenerator;
		}

		public void setSeed(int seed)
		{
			_bodiesGenerator.SetSeed(seed);
		}

		protected override void emitParticles (int particleCount, ParticleFactory factory, ReceiverHandler particleReceiver)
		{
            SSParticle newParticle = factory();
			BodiesFieldGenerator.NewBodyDelegate bodyReceiver = (id, scale, pos, orient) => {
				configureNewParticle(newParticle);
				newParticle.pos = pos;
				newParticle.masterScale *= scale;
				newParticle.orientation += OpenTKHelper.QuaternionToEuler(ref orient);
				particleReceiver(newParticle);
				return true;
			};
			_bodiesGenerator.Generate(particleCount, bodyReceiver);
		}
	}
}

