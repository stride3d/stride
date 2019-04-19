using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;

namespace Xenko.Toolkit.Mathematics
{
    public static partial class Easing
    {
        private const double PiOverTwo = 1.57079637;

        /// <summary>
        /// Performs easing using the specified function.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="function">The easing function to use.</param>
        /// <returns>The amount eased using the specified function.</returns>
        public static double Ease(double amount, EasingFunction function)
        {
            switch (function)
            {
                default:
                case EasingFunction.Linear: return Linear(amount);
                case EasingFunction.QuadraticEaseOut: return QuadraticEaseOut(amount);
                case EasingFunction.QuadraticEaseIn: return QuadraticEaseIn(amount);
                case EasingFunction.QuadraticEaseInOut: return QuadraticEaseInOut(amount);
                case EasingFunction.CubicEaseIn: return CubicEaseIn(amount);
                case EasingFunction.CubicEaseOut: return CubicEaseOut(amount);
                case EasingFunction.CubicEaseInOut: return CubicEaseInOut(amount);
                case EasingFunction.QuarticEaseIn: return QuarticEaseIn(amount);
                case EasingFunction.QuarticEaseOut: return QuarticEaseOut(amount);
                case EasingFunction.QuarticEaseInOut: return QuarticEaseInOut(amount);
                case EasingFunction.QuinticEaseIn: return QuinticEaseIn(amount);
                case EasingFunction.QuinticEaseOut: return QuinticEaseOut(amount);
                case EasingFunction.QuinticEaseInOut: return QuinticEaseInOut(amount);
                case EasingFunction.SineEaseIn: return SineEaseIn(amount);
                case EasingFunction.SineEaseOut: return SineEaseOut(amount);
                case EasingFunction.SineEaseInOut: return SineEaseInOut(amount);
                case EasingFunction.CircularEaseIn: return CircularEaseIn(amount);
                case EasingFunction.CircularEaseOut: return CircularEaseOut(amount);
                case EasingFunction.CircularEaseInOut: return CircularEaseInOut(amount);
                case EasingFunction.ExponentialEaseIn: return ExponentialEaseIn(amount);
                case EasingFunction.ExponentialEaseOut: return ExponentialEaseOut(amount);
                case EasingFunction.ExponentialEaseInOut: return ExponentialEaseInOut(amount);
                case EasingFunction.ElasticEaseIn: return ElasticEaseIn(amount);
                case EasingFunction.ElasticEaseOut: return ElasticEaseOut(amount);
                case EasingFunction.ElasticEaseInOut: return ElasticEaseInOut(amount);
                case EasingFunction.BackEaseIn: return BackEaseIn(amount);
                case EasingFunction.BackEaseOut: return BackEaseOut(amount);
                case EasingFunction.BackEaseInOut: return BackEaseInOut(amount);
                case EasingFunction.BounceEaseIn: return BounceEaseIn(amount);
                case EasingFunction.BounceEaseOut: return BounceEaseOut(amount);
                case EasingFunction.BounceEaseInOut: return BounceEaseInOut(amount);
            }
        }

        /// <summary>
        /// Performs a linear easing.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <returns>The amount eased.</returns>
        /// <remarks>
        /// Modeled after the line y = x
        /// </remarks>
        public static double Linear(double amount)
        {
            return amount;
        }

        /// <remarks>
        /// Modeled after the parabola y = x^2
        /// </remarks>
        public static double QuadraticEaseIn(double amount)
        {
            return amount * amount;
        }

        /// <remarks>
        /// Modeled after the parabola y = -x^2 + 2x
        /// </remarks>
        public static double QuadraticEaseOut(double amount)
        {
            return -(amount * (amount - 2));
        }

        /// <remarks>
        /// Modeled after the piecewise quadratic
        /// y = (1/2)((2x)^2)             ; [0, 0.5]
        /// y = -(1/2)((2x-1)*(2x-3) - 1) ; [0.5, 1]
        /// </remarks>
        public static double QuadraticEaseInOut(double amount)
        {
            if (amount < 0.5)
            {
                return 2 * amount * amount;
            }
            else
            {
                return (-2 * amount * amount) + (4 * amount) - 1;
            }
        }

        /// <remarks>
        /// Modeled after the cubic y = x^3
        /// </remarks>
        public static double CubicEaseIn(double amount)
        {
            return amount * amount * amount;
        }

        /// <remarks>
        /// Modeled after the cubic y = (x - 1)^3 + 1
        /// </remarks>
        public static double CubicEaseOut(double amount)
        {
            double f = (amount - 1);
            return f * f * f + 1;
        }

        /// <remarks>	
        /// Modeled after the piecewise cubic
        /// y = (1/2)((2x)^3)       ; [0, 0.5]
        /// y = (1/2)((2x-2)^3 + 2) ; [0.5, 1]
        /// </remarks>
        public static double CubicEaseInOut(double amount)
        {
            if (amount < 0.5)
            {
                return 4 * amount * amount * amount;
            }
            else
            {
                double f = ((2 * amount) - 2);
                return 0.5 * f * f * f + 1;
            }
        }

        /// <remarks>
        /// Modeled after the quartic x^4
        /// </remarks>
        public static double QuarticEaseIn(double amount)
        {
            return amount * amount * amount * amount;
        }

        /// <remarks>
        /// Modeled after the quartic y = 1 - (x - 1)^4
        /// </remarks>
        public static double QuarticEaseOut(double amount)
        {
            double f = (amount - 1);
            return f * f * f * (1 - amount) + 1;
        }

        /// <remarks>
        /// Modeled after the piecewise quartic
        /// y = (1/2)((2x)^4)        ; [0, 0.5]
        /// y = -(1/2)((2x-2)^4 - 2) ; [0.5, 1]
        /// </remarks>
        public static double QuarticEaseInOut(double amount)
        {
            if (amount < 0.5)
            {
                return 8 * amount * amount * amount * amount;
            }
            else
            {
                double f = (amount - 1);
                return -8 * f * f * f * f + 1;
            }
        }

        /// <remarks>
        /// Modeled after the quintic y = x^5
        /// </remarks>
        public static double QuinticEaseIn(double amount)
        {
            return amount * amount * amount * amount * amount;
        }

        /// <remarks>
        /// Modeled after the quintic y = (x - 1)^5 + 1
        /// </remarks>
        public static double QuinticEaseOut(double amount)
        {
            double f = (amount - 1);
            return f * f * f * f * f + 1;
        }

        /// <remarks>
        /// Modeled after the piecewise quintic
        /// y = (1/2)((2x)^5)       ; [0, 0.5]
        /// y = (1/2)((2x-2)^5 + 2) ; [0.5, 1]
        /// </remarks>
        public static double QuinticEaseInOut(double amount)
        {
            if (amount < 0.5)
            {
                return 16 * amount * amount * amount * amount * amount;
            }
            else
            {
                double f = ((2 * amount) - 2);
                return 0.5 * f * f * f * f * f + 1;
            }
        }

        /// <remarks>
        /// Modeled after quarter-cycle of sine wave
        /// </remarks>
        public static double SineEaseIn(double amount)
        {
            return Math.Sin((amount - 1) * PiOverTwo) + 1;
        }

        /// <remarks>
        /// Modeled after quarter-cycle of sine wave (different phase)
        /// </remarks>
        public static double SineEaseOut(double amount)
        {
            return Math.Sin(amount * PiOverTwo);
        }

        /// <remarks>
        /// Modeled after half sine wave
        /// </remarks>
        public static double SineEaseInOut(double amount)
        {
            return 0.5 * (1 - Math.Cos(amount * Math.PI));
        }

        /// <remarks>
        /// Modeled after shifted quadrant IV of unit circle
        /// </remarks>
        public static double CircularEaseIn(double amount)
        {
            return 1 - Math.Sqrt(1 - (amount * amount));
        }

        /// <remarks>
        /// Modeled after shifted quadrant II of unit circle
        /// </remarks>
        public static double CircularEaseOut(double amount)
        {
            return Math.Sqrt((2 - amount) * amount);
        }

        /// <remarks>	
        /// Modeled after the piecewise circular function
        /// y = (1/2)(1 - Math.Sqrt(1 - 4x^2))           ; [0, 0.5]
        /// y = (1/2)(Math.Sqrt(-(2x - 3)*(2x - 1)) + 1) ; [0.5, 1]
        /// </remarks>
        public static double CircularEaseInOut(double amount)
        {
            if (amount < 0.5)
            {
                return 0.5 * (1 - Math.Sqrt(1 - 4 * (amount * amount)));
            }
            else
            {
                return 0.5 * (Math.Sqrt(-((2 * amount) - 3) * ((2 * amount) - 1)) + 1);
            }
        }

        /// <remarks>
        /// Modeled after the exponential function y = 2^(10(x - 1))
        /// </remarks>
        public static double ExponentialEaseIn(double amount)
        {
            return (amount == 0.0) ? amount : Math.Pow(2, 10 * (amount - 1));
        }

        /// <remarks>
        /// Modeled after the exponential function y = -2^(-10x) + 1
        /// </remarks>
        public static double ExponentialEaseOut(double amount)
        {
            return (amount == 1.0) ? amount : 1 - Math.Pow(2, -10 * amount);
        }

        /// <remarks>
        /// Modeled after the piecewise exponential
        /// y = (1/2)2^(10(2x - 1))         ; [0,0.5)
        /// y = -(1/2)*2^(-10(2x - 1))) + 1 ; [0.5,1]
        /// </remarks>
        public static double ExponentialEaseInOut(double amount)
        {
            if (amount == 0.0 || amount == 1.0) return amount;

            if (amount < 0.5)
            {
                return 0.5 * Math.Pow(2, (20 * amount) - 10);
            }
            else
            {
                return -0.5 * Math.Pow(2, (-20 * amount) + 10) + 1;
            }
        }

        /// <remarks>
        /// Modeled after the damped sine wave y = sin(13pi/2*x)*Math.Pow(2, 10 * (x - 1))
        /// </remarks>
        public static double ElasticEaseIn(double amount)
        {
            return Math.Sin(13 * PiOverTwo * amount) * Math.Pow(2, 10 * (amount - 1));
        }

        /// <remarks>
        /// Modeled after the damped sine wave y = sin(-13pi/2*(x + 1))*Math.Pow(2, -10x) + 1
        /// </remarks>
        public static double ElasticEaseOut(double amount)
        {
            return Math.Sin(-13 * PiOverTwo * (amount + 1)) * Math.Pow(2, -10 * amount) + 1;
        }

        /// <remarks>
        /// Modeled after the piecewise exponentially-damped sine wave:
        /// y = (1/2)*sin(13pi/2*(2*x))*Math.Pow(2, 10 * ((2*x) - 1))      ; [0,0.5]
        /// y = (1/2)*(sin(-13pi/2*((2x-1)+1))*Math.Pow(2,-10(2*x-1)) + 2) ; [0.5, 1]
        /// </remarks>
        public static double ElasticEaseInOut(double amount)
        {
            if (amount < 0.5)
            {
                return 0.5 * Math.Sin(13 * PiOverTwo * (2 * amount)) * Math.Pow(2, 10 * ((2 * amount) - 1));
            }
            else
            {
                return 0.5 * (Math.Sin(-13 * PiOverTwo * ((2 * amount - 1) + 1)) * Math.Pow(2, -10 * (2 * amount - 1)) + 2);
            }
        }

        /// <remarks>
        /// Modeled after the overshooting cubic y = x^3-x*sin(x*pi)
        /// </remarks>
        public static double BackEaseIn(double amount)
        {
            return amount * amount * amount - amount * Math.Sin(amount * Math.PI);
        }

        /// <remarks>
        /// Modeled after overshooting cubic y = 1-((1-x)^3-(1-x)*sin((1-x)*pi))
        /// </remarks>	
        public static double BackEaseOut(double amount)
        {
            double f = (1 - amount);
            return 1 - (f * f * f - f * Math.Sin(f * Math.PI));
        }

        /// <remarks>
        /// Modeled after the piecewise overshooting cubic function:
        /// y = (1/2)*((2x)^3-(2x)*sin(2*x*pi))           ; [0, 0.5)
        /// y = (1/2)*(1-((1-x)^3-(1-x)*sin((1-x)*pi))+1) ; [0.5, 1]
        /// </remarks>
        public static double BackEaseInOut(double amount)
        {
            if (amount < 0.5)
            {
                double f = 2 * amount;
                return 0.5 * (f * f * f - f * Math.Sin(f * Math.PI));
            }
            else
            {
                double f = (1 - (2 * amount - 1));
                return 0.5 * (1 - (f * f * f - f * Math.Sin(f * Math.PI))) + 0.5;
            }
        }

        /// <remarks>
        /// </remarks>
        public static double BounceEaseIn(double amount)
        {
            return 1 - BounceEaseOut(1 - amount);
        }

        /// <remarks>
        /// </remarks>
        public static double BounceEaseOut(double amount)
        {
            if (amount < 4 / 11.0)
            {
                return (121 * amount * amount) / 16.0;
            }
            else if (amount < 8 / 11.0)
            {
                return (363 / 40.0 * amount * amount) - (99 / 10.0 * amount) + 17 / 5.0;
            }
            else if (amount < 9 / 10.0)
            {
                return (4356 / 361.0 * amount * amount) - (35442 / 1805.0 * amount) + 16061 / 1805.0;
            }
            else
            {
                return (54 / 5.0 * amount * amount) - (513 / 25.0 * amount) + 268 / 25.0;
            }
        }

        /// <remarks>
        /// </remarks>
        public static double BounceEaseInOut(double amount)
        {
            if (amount < 0.5)
            {
                return 0.5 * BounceEaseIn(amount * 2);
            }
            else
            {
                return 0.5 * BounceEaseOut(amount * 2 - 1) + 0.5;
            }
        }
    }
}
