using System;
using System.Dynamic;
using OpenTK;

namespace SimpleScene.Util
{
    public abstract class AverageArithmeticProvider<T>
    {
        public abstract T Add(T left, T right);
        public abstract T Subtract(T left, T right);
        public abstract T Divide(T left, int right);
        public abstract T Zero();

        public void SubtractEquals(T left, T right) {
            left = Subtract(left, right);
        }
        public void AddEquals(T left, T right) {
            left = Add(left, right);
        }
    }

    // Class for keeping a running average
    // Implemented via a circular queue
    public class RunningAverage<T>
    {
        static protected AverageArithmeticProvider<T> s_mathProvider;
        protected readonly T[] m_history;
        protected T m_sum;
        protected int m_insertedCount = 0;  // number of items inserted
        protected int m_nextIdx = 0;        // next item will be inserted here

        static RunningAverage() {
            if (typeof(T) == typeof(int)) {
                s_mathProvider = new IntArithmeticProvider() as AverageArithmeticProvider<T>;
            } else if (typeof(T) == typeof(float)) {
                s_mathProvider = new FloatArithmeticProvider() as AverageArithmeticProvider<T>;
            } else if (typeof(T) == typeof(double)) {
                s_mathProvider = new DoubleArithmeticProvider() as AverageArithmeticProvider<T>;
            } else if (typeof(T) == typeof(Vector3)) {
                s_mathProvider = new Vector3ArithmeticProvider() as AverageArithmeticProvider<T>;
            } else {
                string msg = "Type " + typeof(T).ToString() + " is not supported by RunningAverage.";
                throw new InvalidOperationException(msg);
            }
        }

        public RunningAverage(int maxNumItems) {
            m_history = new T[maxNumItems];
            m_sum = s_mathProvider.Zero();
        }

        public T Average {
            get {
                if (m_insertedCount == 0) {
                    return s_mathProvider.Zero();
                } else {
                    return s_mathProvider.Divide(m_sum, m_insertedCount);
                }
            }
        }

        public void Push(T item) {
            if (m_insertedCount == m_history.Length) {
                // full. remove the last item first
                s_mathProvider.SubtractEquals(m_sum, item);
            }
            m_history[m_nextIdx] = item;
            m_sum += (dynamic)item;
            ++m_insertedCount;
            ++m_nextIdx;
            if (m_nextIdx >= m_history.Length) {
                m_nextIdx = 0;
            }
        }
    }

    class FloatArithmeticProvider : AverageArithmeticProvider<float>
    {
        public override float Add(float left, float right) {
            return left + right;
        }
        public override float Subtract(float left, float right) {
            return left - right;
        }
        public override float Divide(float left, int right) {
            return left / (float)right;
        }
        public override float Zero() {
            return 0.0f;
        }
    }

    class DoubleArithmeticProvider : AverageArithmeticProvider<double>
    {
        public override double Add(double left, double right) {
            return left + right;
        }
        public override double Subtract(double left, double right) {
            return left - right;
        }
        public override double Divide(double left, int right) {
            return left / (double)right;
        }
        public override double Zero() {
            return 0.0;
        }
    }

    class IntArithmeticProvider : AverageArithmeticProvider<int>
    {
        public override int Add(int left, int right) {
            return left + right;
        }
        public override int Subtract(int left, int right) {
            return left - right;
        }
        public override int Divide(int left, int right) {
            return left / (int)right;
        }
        public override int Zero() {
            return 0;
        }
    }

    class Vector3ArithmeticProvider : AverageArithmeticProvider<Vector3>
    {
        public override Vector3 Add(Vector3 left, Vector3 right) {
            return left + right;
        }
        public override Vector3 Subtract(Vector3 left, Vector3 right) {
            return left - right;
        }
        public override Vector3 Divide(Vector3 left, int right) {
            return left / right;
        }
        public override Vector3 Zero() {
            return new Vector3(0.0f, 0.0f, 0.0f);
        }
    }
}
