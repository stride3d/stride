using System;

namespace Open.Nat
{
	internal class Guard
	{
		private Guard()
		{
		}

		internal static void IsInRange(int paramValue, int lowerBound, int upperBound, string paramName)
		{
			if (paramValue < lowerBound || paramValue > upperBound)
				throw new ArgumentOutOfRangeException(paramName);
		}

		internal static void IsTrue(bool exp, string paramName)
		{
			if (!exp)
				throw new ArgumentOutOfRangeException(paramName);
		}

		internal static void IsNotNull(object obj, string paramName)
		{
			if(obj == null) throw new ArgumentNullException(paramName);
		}
	}
}