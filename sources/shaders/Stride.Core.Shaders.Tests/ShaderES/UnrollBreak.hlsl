void VSMain(int start2)
{
    float var = 0.0;

	[unroll]
    for (int i = 0; i < 7; ++i)
    {
        var += 1.0;
		if (var > 5.5)
		{
			break;
			var += 2.0;
		}
    }

	[unroll]
    for (int i = 0; i < 7; ++i)
    {
        var += 1.0;
		if (var > 5.5)
			break;
		var += 2.0;

		if (var > 0.3)
			break;
		var -= 1.0;
    }

	[unroll]
    for (int i = 0; i < 7; ++i)
    {
        var += 1.0;
		if (var > 5.5)
			break;
		var += 2.0;
		
		[unroll]
		for (int j = 0; j < 7; ++j)
		{
			var += 1.0;
			if (var > 5.5)
				break;
			var += 2.0;
		}

		if (var > 0.3)
			break;
		var -= 1.0;
    }
    
	[unroll]
    for (int i = 0; i < 7; ++i)
    {
        var += 1.0;
		if (var > 5.5)
			continue;
		var += 2.0;

		if (var > 0.3)
			break;
		var -= 1.0;
    }
}
