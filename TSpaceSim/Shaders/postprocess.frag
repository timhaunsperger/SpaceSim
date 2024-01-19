#version 460 core
out vec4 FragColor;
in vec2 texCoord;

uniform sampler2D colorTex;
uniform sampler2D posTex;
uniform sampler2D normTex;
uniform sampler2D depthTex;
uniform sampler3D cloudTex;

uniform vec3 viewPos;
uniform vec3 center;
uniform float oceanRadius;
uniform vec3 oceanCol;

uniform int atmDepthSteps;
uniform float atmRadius;
uniform vec3 rgbScatterFactors;
uniform float scatterStrength;
uniform float cloudScatterStr;
uniform vec3 sunPos;
uniform vec3 sunCol = vec3(1,0.95294,0.9294) * 2;

// Return sphereEnterDist, distThroughSphere
// If inside, dstToSphere = 0
// If non-intersection, returns (0,0)
vec2 raySphere(vec3 center, float radius, vec3 rayOrigin, vec3 lookDir){

    vec3 offset = rayOrigin - center;

    // Solve quadratic equation for sphere intersections
    float a = 1;
    float b = 2 * dot(offset, lookDir);
    float c = dot(offset, offset) - radius * radius;
    float discriminant = b * b - (4 * c);

    if(discriminant > 0){
        float sqrtDsc = sqrt(discriminant);
        float near = max(0, (-b - sqrtDsc) / 2);
        float far = (-b + sqrtDsc) / 2;
        if(far > 0){
            return vec2(near, far - near);
        }
    }

    return vec2(0,0);
}

float atmDensity(vec3 samplePos){
    float altitude = length(samplePos - center) - oceanRadius;
    float altitudePercent = altitude / (atmRadius - oceanRadius);
    return exp(-altitudePercent * 4) * (1 - altitudePercent) * scatterStrength;
}

// Returns (optDepth, cloudDepth)
vec3 opticalDepth(vec3 rayOrigin, vec3 rayDir, float rayLen){
    float stepSize= rayLen / (7 - 1);
    vec3 optDepth = vec3(0);
    vec3 samplePoint = rayOrigin;
    float stepDist = 0;
    
    for(int i = 0; i < 7; i++){
        float density = atmDensity(samplePoint);
        float cloud = texture(cloudTex, (samplePoint / atmRadius + vec3(1)) / 2).x;

        optDepth += (density * rgbScatterFactors + cloud * cloudScatterStr)  * stepSize;
        samplePoint += stepSize * rayDir;
    }
    return optDepth;
}

vec3 calcScatter(vec3 rayOrigin, vec3 rayDir, float rayLen, vec3 originalCol){
    float stepSize = rayLen / (atmDepthSteps - 1);
    vec3 scatteredLight = vec3(0);
    vec3 scatterPoint = rayOrigin;
    vec3 sunRayOptDepth = vec3(0);
    
    vec3 transmittance = vec3(1);
    vec3 inScattering;

    for (int i = 0; i < atmDepthSteps; i++) {
        vec3 sunDir = normalize(sunPos - scatterPoint);
        float sunRayLen = raySphere(center, atmRadius, scatterPoint, sunDir).y;
        
        sunRayOptDepth = opticalDepth(scatterPoint, sunDir, sunRayLen);
        float density = atmDensity(scatterPoint);
        
        float cloudDensity = texture(cloudTex, (scatterPoint / atmRadius + vec3(1)) / 2).x * cloudScatterStr;
        
        float cosine = dot(sunDir, rayDir);
        float cosSqr = cosine * cosine;
        float phaseFuncRaleigh = 0.75 * (1 + cosSqr);
        float phaseFuncMie = 0.256 * (1 + cosSqr)/(1.5625-1.5*cosine);
        //float phaseFuncMie = 0.4 * (1 + cosine * cosine)/(1.36-1.2*cosine);
        //float phaseFuncMie = 0.101423 * (1 + cosine * cosine)/(1.81-1.8*cosine);

        vec3 cloudScatter = vec3(cloudDensity) * stepSize;
        vec3 atmScatter = rgbScatterFactors * density * stepSize;
        
        transmittance *= exp(-(atmScatter + cloudScatter));
        inScattering += exp(-sunRayOptDepth) * transmittance * (cloudScatter * phaseFuncMie + atmScatter * phaseFuncRaleigh);
        
        if(transmittance.x < 0.05){break;}
        
        scatterPoint += rayDir * stepSize;
    }
    vec3 surfaceIllumination = exp(-sunRayOptDepth);
    return (originalCol * surfaceIllumination * transmittance + inScattering) * sunCol;
}

float LinearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0; // back to NDC 
    return (2.0 * 0.01 * 100) / (100 + 0.01 - z * (100 - 0.01));
}

void main()
{
    vec3 fragPos = texture(posTex, texCoord).xyz;
    float depth = length(fragPos - viewPos);
    
    vec3 viewDir = normalize(fragPos - viewPos);
    vec3 color = texture(colorTex, texCoord).xyz;
    
    
    vec2 oceanInfo = raySphere(center, oceanRadius, viewPos, viewDir);

    float oceanDist = oceanInfo.x;
    float dstThroughOcean = oceanInfo.y;
    float oceanViewDepth = min(depth - oceanDist, dstThroughOcean);
    
//    if(depth > 0){
//        FragColor = texture(cloudTex, vec3(texCoord, 0.5));
//        return;
//    }

    oceanViewDepth = max(oceanViewDepth, 0);
    vec3 sunDir = normalize(sunPos - fragPos);
    
    vec2 atmInfo = raySphere(center, atmRadius, viewPos, viewDir);
    float atmViewDepth = min(depth - atmInfo.x, atmInfo.y);

    if(oceanViewDepth > 0){

        float opacity = 1 - exp(-oceanViewDepth * 100 / oceanRadius);
        fragPos = (viewPos + viewDir * oceanDist);
        depth = length(fragPos - viewPos);
        vec3 norm = normalize(fragPos - center);

        sunDir = normalize(sunPos - fragPos);
        vec3 reflectDir = reflect(sunDir, norm);

        float diffuse = max(dot(norm, sunDir), 0.0);
        vec3 specular = pow(max(dot(viewDir, reflectDir), 0.0), 32) * vec3(1);

        color = (color * (1 - opacity) + oceanCol * opacity) + specular;
    }
    
    if(atmViewDepth > 0){
        vec3 atmEnterPt = viewPos + atmInfo.x * viewDir;
        color = calcScatter(atmEnterPt, viewDir, atmViewDepth, color.xyz);
    }
    
    // tone mapping
    color = pow(vec3(1) - exp(-color * 3), vec3(1/2.2));
    FragColor = vec4(color, 1);
}