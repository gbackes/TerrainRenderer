              
float2 gradienthash2D(float2 x)
{
    const float2 k = float2(0.3183099, 0.3678794);
    x = x * k + k.yx;
    return -1.0 + 2.0 * frac(16.0 * k * frac(x.x * x.y * (x.x + x.y)));
}

float gradientnoise2D(in float2 p, float seed = 0)
{
    float2 i = floor(p);
    float2 f = frac(p);

 // Get weights from the coordinate fraction
    float2 w = f * f * f * (f * (f * 6 - 15) + 10); // 6f^5 - 15f^4 + 10f^3
    float4 w4 = float4(1, w.x, w.y, w.x * w.y);


    //Gradients
    float2 ga = gradienthash2D(i + seed + float2(0.0, 0.0));
    float2 gb = gradienthash2D(i + seed + float2(1.0, 0.0));
    float2 gc = gradienthash2D(i + seed + float2(0.0, 1.0));
    float2 gd = gradienthash2D(i + seed + float2(1.0, 1.0));
                               
    float4 g1 = float4(ga, gc);
    float4 g2 = float4(gb, gd);

    //Projections -  // Evaluate the four lattice gradients at p
    float va = dot(ga, f - float2(0.0, 0.0));
    float vb = dot(gb, f - float2(1.0, 0.0));
    float vc = dot(gc, f - float2(0.0, 1.0));
    float vd = dot(gd, f - float2(1.0, 1.0));

     // Bi-linearly blend between the gradients, using w4 as blend factors.
    float4 grads = float4(va, vb - va, vc - va, va - vb - vc + vd);
    float n = dot(grads, w4);
    return n;
}


float gradientfbm2D(float2 p, int octaves, float freq, float lacunarity, float gain, float amp)
{
    float sum = 0;
    float range = 0;
    for (int i = 0; i < octaves; i++)
    {
        range += amp;
        sum += gradientnoise2D(p * freq) * amp;
        freq *= lacunarity;
        amp *= gain;
    }
    sum *= 8;
    return sum / range;
}

float ridgegradientnoise2D(float2 p, float seed = 0)
{
    return 1.0 - abs(gradientnoise2D(p, seed));
}

float ridgegradientfbm2D(float2 p, int octaves, float freq, float lacunarity, float gain, float amp)
{
    float sum = 0;
    for (int i = 0; i < octaves; i++)
    {
        sum += ridgegradientnoise2D(p * freq) * amp;
        freq *= lacunarity;
        amp *= gain;
    }
    return sum;
}
     
float gradientnoiseDeriv2D(in float2 p, out float2 dn, out float3 dnn, float seed = 0)
{
    float2 i = floor(p);
    float2 f = frac(p);

 // Get weights from the coordinate fraction
    float2 w = f * f * f * (f * (f * 6 - 15) + 10); // 6x^5 - 15x^4 + 10x^3
    
    float4 w4 = float4(1, w.x, w.y, w.x * w.y); //Bilinear Interpolation purpose 

    
    // Get the derivative dw/df
    float2 dw = f * f * (f * (f * 30 - 60) + 30); // 30x^4 - 60x^3 + 30x^2
            
    
    float4 dw4x = float4(0, dw.x, 0, dw.x * w.y); //Bilinear Interpolation purpose 
    float4 dw4y = float4(0, 0, dw.y, w.x * dw.y); //Bilinear Interpolation purpose 
    
    //Second derivative dw/df
    float2 d2w = f * (f * (f * 120 - 180) + 60);

    float4 d2w4x = float4(0, d2w.x, 0, d2w.x * w.y); //Bilinear Interpolation purpose 
    float4 d2w4y = float4(0, 0, d2w.y, w.x * d2w.y); //Bilinear Interpolation purpose 
    float4 d2w4xy = float4(0, 0, 0, d2w.x * d2w.y);
    //Gradients
    float2 ga = gradienthash2D(i + seed + float2(0.0, 0.0));
    float2 gb = gradienthash2D(i + seed + float2(1.0, 0.0));
    float2 gc = gradienthash2D(i + seed + float2(0.0, 1.0));
    float2 gd = gradienthash2D(i + seed + float2(1.0, 1.0));
                               
    float4 g1 = float4(ga, gc);
    float4 g2 = float4(gb, gd);

    //Projections -  // Evaluate the four lattice gradients at p
    float va = dot(ga, f - float2(0.0, 0.0));
    float vb = dot(gb, f - float2(1.0, 0.0));
    float vc = dot(gc, f - float2(0.0, 1.0));
    float vd = dot(gd, f - float2(1.0, 1.0));

     // Bi-linearly blend between the gradients, using w4 as blend factors.
    float4 grads = float4(va, vb - va, vc - va, va - vb - vc + vd);
    float n = dot(grads, w4);


    float2 gradsInterp = ga + w.x * (gb - ga) + w.y * (gc - ga) + w.x * w.y * (ga - gb - gc + gd);

     // Calculate pseudo derivates
    float dx = dot(grads, dw4x);
    float dy = dot(grads, dw4y);
                                                    
    float dxx = dot(grads, d2w4x);
    float dyy = dot(grads, d2w4y);
    float dxy = dot(grads, d2w4xy);

    // Calculate the derivatives  // The corret would be add the gradsInterp - http://www.decarpentier.nl/scape-procedural-extensions
    dn = float2(dx, dy);//+gradsInterp;

    dnn = float3(dxx, dyy, dxy); //+ float3(gradsInterp, gradsInterp.x * gradsInterp.y);
           


    // Return the noise value, roughly normalized in the range [-1, 1]
    return n * 1.5;
}

float Concavity(float2 dn, float3 dnn)
{                    
    float kv = -2.0f * (dn.x * dn.x * dnn.x + dn.y * dn.y * dnn.y + dn.x * dn.y * dnn.z);
    kv /= dn.x * dn.x + dn.y * dn.y;
    float kh = -2.0f * (dn.y * dn.y * dnn.x + dn.x * dn.x * dnn.y - dn.x * dn.y * dnn.z);
    kh /= dn.x * dn.x + dn.y * dn.y;

    return (kh + kv) * 0.5f;

}

float3 terrainNoise(float2 position, int iseed, int ioctaves, float fperturbFeatures, float fsharpness, float faltitudeErosion,
                float fridgeErosion, float fslopeErosion, float fconcavityErosion, float ffrequency, float flacunarity, float famplitude, float fgain)
{
    float fsum = 0;
    float2 fdsum = float2(0, 0);
    float2 slopeErosionDerivativeSum = float2(0, 0);
    float2 ridgeErosionDerivativeSum = float2(0, 0);
    float2 perturbDerivativeSum = float2(0, 0);
    float2 octavePosition = position;
    float fcurrentGain = fgain;
    float frange = 0;
    float fdampedAmplitude = famplitude;
    float3 derivative2sum = float3(0, 0, 0);
    float2 derivativesum = float2(0, 0);
    for (int i = 0; i < ioctaves; i++)
    {
                 
        //NOISE 
        float3 derivative2;
        float2 derivative;    
        float ffeatureNoise = gradientnoiseDeriv2D(octavePosition, derivative, derivative2, iseed);


        //SHARPNESS    
        float fbillowNoise = abs(ffeatureNoise);
        float fridgedNoise = -abs(ffeatureNoise);
       
        ffeatureNoise = lerp(ffeatureNoise, fbillowNoise, max(0.0, fsharpness));
        ffeatureNoise = lerp(ffeatureNoise, fridgedNoise, abs(min(0.0, fsharpness)));       
        
        //SLOPE EROSION                                           
        slopeErosionDerivativeSum += derivative * fslopeErosion;
        ridgeErosionDerivativeSum += derivative * fridgeErosion;
        fdsum += derivative;
              
        float slopeErosion = (1.0f / (1.0f + dot(slopeErosionDerivativeSum, slopeErosionDerivativeSum)));

        fsum += ffeatureNoise * fdampedAmplitude * slopeErosion;
                                   
        frange += fdampedAmplitude;

        ffrequency *= flacunarity;


        //Concavity Erosion       //WORK ON THIS
        float concavity = Concavity(derivative, derivative2);
        float concavitySlope = (1.0f / (1.0f + dot(derivative, derivative)));
        float fdampedGain = lerp(fcurrentGain, fcurrentGain * (1.0f / (1.0f + abs(min(concavity, 0)))), fconcavityErosion * concavitySlope);

        //AMPLITUDE DAMPING
        famplitude *= lerp(fdampedGain, fdampedGain * smoothstep(0, 1, fsum), faltitudeErosion);

        fdampedAmplitude = famplitude * (1.0f - (fridgeErosion / (1.0f + dot(ridgeErosionDerivativeSum, ridgeErosionDerivativeSum))));
        
 

        //DOMAIN PERTURB
        octavePosition = (position * ffrequency) + perturbDerivativeSum;

        perturbDerivativeSum += derivative * fperturbFeatures;
                                                             
    }
    fsum = fsum / frange;
    fsum = fsum * pow(abs(fsum), 0);
    fsum *= 8;
    return float3(fsum, fdsum); // / frange;
}

float remap(float org_val, float org_min, float org_max, float new_min, float new_max)
{
    return new_min + saturate(((org_val - org_min) / (org_max - org_min)) * (new_max - new_min));
}

float riverNoisefbm2d(float2 p, float seed, int octaves, float freq, float lacunarity, float gain, float amp)
{
    float sum = 0;
    float range = 0;
                             
    float influence = 1;
    for (int i = 0; i < octaves; i++)
    {
        float noise = ridgegradientnoise2D(p * freq, seed);
        noise *= noise * noise * noise * noise * noise * noise * noise;
        influence *= noise * amp;
        sum += influence * amp;
        freq *= lacunarity;
        range += amp;
        amp *= gain;
    }
    sum /= range;
    return sum;
}