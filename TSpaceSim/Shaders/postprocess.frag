#version 460 core
out vec4 FragColor;
in vec2 texCoord;

uniform sampler2D colorTex;
uniform sampler2D posTex;
uniform sampler2D normTex;
uniform sampler2D depthTex;

uniform vec3 viewPos;
uniform vec3 center;
uniform float oceanRadius;
uniform vec3 oceanCol;

uniform int atmDepthSteps;
uniform float atmRadius;
uniform vec3 rgbScatterFactors;
uniform float scatterStrength;
uniform vec3 sunPos;

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
    return exp(-altitudePercent * 3) * (1 - altitudePercent) * scatterStrength;
}

float opticalDepth(vec3 rayOrigin, vec3 rayDir, float rayLen){
    float stepSize = rayLen / (atmDepthSteps - 1);
    float optDepth = 0;
    vec3 samplePoint = rayOrigin;
    
    for (int i = 0; i < atmDepthSteps; i++) {
        optDepth += atmDensity(samplePoint) * stepSize;
        samplePoint += stepSize * rayDir;
    }
    return optDepth;
}

vec3 calcScatter(vec3 rayOrigin, vec3 rayDir, float rayLen, vec3 originalCol){
    float stepSize = rayLen / (atmDepthSteps - 1);
    vec3 scatteredLight = vec3(0);
    vec3 scatterPoint = rayOrigin;
    float viewRayOptDepth = 0;

    for (int i = 0; i < atmDepthSteps; i++) {
        float density = atmDensity(scatterPoint);

        vec3 sunDir = normalize(sunPos - scatterPoint);
        float sunRayLen = raySphere(center, atmRadius, scatterPoint, sunDir).y;
        
        float sunRayOptDepth = opticalDepth(scatterPoint, sunDir, sunRayLen);
        float viewRayOptDepth = opticalDepth(scatterPoint, -rayDir, i * stepSize);
        
        float cosine = dot(sunDir, rayDir);
        float phaseFuncScale = 3.0/4.0 * (1 + cosine * cosine);

        scatteredLight += density * exp(-(sunRayOptDepth + viewRayOptDepth) * rgbScatterFactors) * stepSize * rgbScatterFactors * phaseFuncScale;
        scatterPoint += rayDir * stepSize;
    }
    float originalTransmittence = exp(-viewRayOptDepth);
    
    return originalTransmittence * originalCol + scatteredLight;
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
    vec4 color = texture(colorTex, texCoord);
    
    vec2 oceanInfo = raySphere(center, oceanRadius, viewPos, viewDir);

    float oceanDist = oceanInfo.x;
    float dstThroughOcean = oceanInfo.y;
    float oceanViewDepth = min(depth - oceanDist, dstThroughOcean);
    
    oceanViewDepth = max(oceanViewDepth, 0);
    vec3 sunDir = normalize(sunPos - fragPos);
    if(oceanViewDepth > 0){

        float opacity = 1 - exp(-oceanViewDepth * 40 / oceanRadius);
        vec3 oceanPos = (viewPos + viewDir * oceanDist);
        vec3 norm = normalize(oceanPos - center);
        sunDir = normalize(sunPos - oceanPos);
        vec3 reflectDir = reflect(sunDir, norm);
                
        float diffuse = max(dot(norm, sunDir), 0.0);
        vec4 specular = pow(max(dot(viewDir, reflectDir), 0.0), 32) * vec4(1);
        
        color = (color * (1 - opacity) + vec4(oceanCol, 1) * opacity) * diffuse + specular;
    }
    else{
        vec3 norm = texture(normTex, texCoord).xyz;
        float diffuse = max(dot(norm, sunDir), 0.0);
        color = color * diffuse;
    }
    
    vec2 atmInfo = raySphere(center, atmRadius, viewPos, viewDir);
    float atmViewDepth = min(depth - atmInfo.x, atmInfo.y);

    if(atmViewDepth > 0){
        vec3 atmEnterPt = viewPos + atmInfo.x * viewDir;
        color = vec4(calcScatter(atmEnterPt, viewDir, atmViewDepth, color.xyz), 1);
    }
    
    FragColor = color;
}