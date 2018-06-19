const float nonUnroll[3] = { 0.01, 0.01, 0.01 };

void VSMain(int start2)
{
    float filter[7] = { 0.03007832, 0.10498366, 0.22225042, 0.28537519, 0.22225042, 0.10498366, 0.03007832 };
    
    float res = nonUnroll[0];
    
    [unroll]
    for (int i = 0; i < 7; ++i)
    {
        res += filter[i];
    }
    
    [unroll]
    for (int i = 0; i < 7; i = i+1)
    {
        res += filter[i];
    }
    
    [unroll]
    for (int i = 0; i < 7; i += 1)
    {
        res += filter[i];
    }
    
    [unroll]
    for (int i = 0; i < 7; i += 2)
    {
        res += filter[i];
    }
    
    [unroll]
    for (int i = 0; i < 7; i += 3)
    {
        res += filter[i];
    }
    
    [unroll]
    for (int i = 6; i >= 0; i -= 1)
    {
        res += filter[i];
    }
    
    [unroll]
    for (int i = 4; i == 4; i -= 1)
    {
        res += filter[i];
    }
    
    [unroll]
    for (int i = 3; i == 4; i -= 1)
    {
        res += filter[i];
    }
    
    [unroll]
    for (int i = 6; i >= 0; ++i) // produces an error
    {
        res += filter[i];
    }
    
    int start = 0;
    [unroll]
    for (int i = start; i < 7; i = i+1)
    {
        res += filter[i];
    }
    
    [unroll]
    for (int i = start2; i < 7; i = i+1)
    {
        res += filter[i];
    }
}
