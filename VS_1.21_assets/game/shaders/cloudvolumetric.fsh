#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

uniform mat4 iMvpMatrix;
uniform sampler2D depthTex;
uniform sampler2D cloudMap;
uniform sampler2D cloudCol;
uniform float cloudMapWidth;
uniform vec3 cloudOffset;
uniform int frame;
uniform float time;
uniform int FrameWidth;
uniform float PerceptionEffectIntensity;

in vec2 uv;
in vec2 ndc;

layout(location = 0) out vec4 outAccu;
layout(location = 1) out vec4 outReveal;
layout(location = 2) out vec4 outGlow;

vec3 hash(vec3 p){

    // https://www.shadertoy.com/view/NtjyWw

    const uint k = 1103515245U;
    uvec3 x = floatBitsToUint(p);
    x = ((x>>8U)^x.yzx)*k;
    x = ((x>>8U)^x.yzx)*k;
    x = ((x>>8U)^x.yzx)*k;
    return vec3(x)/float(0xffffffffU);

}

float noise(vec3 p){
    vec3 f = smoothstep(0.0, 1.0, fract(p));
    vec3 x = floor(p);
    return mix(mix(mix(hash(x + vec3(0, 0, 0)).x,
                       hash(x + vec3(1, 0, 0)).x, f.x),
                   mix(hash(x + vec3(0, 1, 0)).x,
                       hash(x + vec3(1, 1, 0)).x, f.x), f.y),
               mix(mix(hash(x + vec3(0, 0, 1)).x,
                       hash(x + vec3(1, 0, 1)).x, f.x),
                   mix(hash(x + vec3(0, 1, 1)).x,
                       hash(x + vec3(1, 1, 1)).x, f.x), f.y), f.z);
}

float octave(vec3 p){
    return (noise(p * 2.0) * 0.66 + noise(p * 6.0) * 0.33) * 2.0 - 1.0;
}

mat2 rot(float n){
    return mat2(cos(n), -sin(n), sin(n), cos(n));
}

vec3 warp(vec3 d, float f){
    if(f < 0.0001) return d;
    d.xz *= rot(octave(d * 2.0 + time * 0.05) * f);
    d.xy *= rot(octave(d * 1.5 + time * 0.04) * f);
    d.zy *= rot(octave(d * 1.5 - time * 0.04) * f);
    return normalize(d);
}

vec3 curve(vec3 d, float f){
    d.xy *= rot(d.x * f);
    d.zy *= rot(d.z * f);
    return normalize(d);
}

float luma(vec3 c){
    return dot(c, vec3(0.3, 0.6, 0.1));
}

#include dither.fsh

vec4 dither(vec4 colour, float depth){

    vec4 noise = NoiseFromPixelPosition(ivec2(gl_FragCoord.xy), frame, FrameWidth);
    noise = noise * 128.0 * 0.5 + 0.5;
    float l = luma(colour.rgb);
    colour /= l;
    return (floor(colour * depth + noise) / depth) * l;

}

float gentleNoise(){

    float n = NoiseFromPixelPosition(ivec2(gl_FragCoord.xy), frame + 256, FrameWidth).r;
//  n = (n * 128.0 * 0.5 + 0.5) / 128.0;
    float s = FrameWidth / 240.0;
    return n * s;

}

void drawPixel(vec4 color){

    float weight = luma(color.rgb / color.a);

    outAccu = color * weight * weight * 6000.0;
    outReveal.r = color.a;
    outGlow = vec4(0.0, 0.0, 0.0, color.a);

}

vec3 unproject(vec4 x){
    return x.xyz / x.w;
}

float volume(float o, float d, vec2 m, float t, float f){
    m = (m - o) / d;
    return 1.0 - exp(-max(0.0, min(max(m.x, m.y), t) - max(0.0, min(m.x, m.y))) * f);
}

vec2 intersect(float o, float d, vec2 m){
    m = (m - o) / d;
    float near = min(m.x, m.y);
    float far = max(m.x, m.y);
    if(near > far || far < 0.0) return vec2(-1.0);
    return vec2(max(0.0, near), max(0.0, far - max(0.0, near)));
}

vec4 traverse(vec3 o, vec3 d, float far){

    ivec2 p = ivec2(floor(o.xz));
    ivec2 istep = ivec2(sign(d.xz));
    vec2 tdelta, tmax;
    tdelta.x = 1.0 / max(0.0001, abs(d.x));
    tdelta.y = 1.0 / max(0.0001, abs(d.z));
    tmax.x = (d.x > 0.0 ? floor(o.x) + 1.0 - o.x : o.x - floor(o.x)) * tdelta.x;
    tmax.y = (d.z > 0.0 ? floor(o.z) + 1.0 - o.z : o.z - floor(o.z)) * tdelta.y;
    float t = 0.0;
    vec4 k = vec4(0.0);

    for(int i = 0; i < 200; i++){

        vec4 map = texelFetch(cloudMap, p, 0);

        if(map.r > 0.0){

            vec4 col = texelFetch(cloudCol, p, 0);

            k += (1.0 - k.a)
               * col
               * volume(
                     o.y + d.y * t,
                     d.y,
                     map.ba,
                     min(far, min(tmax.x, tmax.y)) - t,
                     map.r
                 );

            if(k.a > 0.99) break;

        }

        if(tmax.x < tmax.y){
            p.x += istep.x;
            t = tmax.x;
            tmax.x += tdelta.x;
        }else{
            p.y += istep.y;
            t = tmax.y;
            tmax.y += tdelta.y;
        }

        if(t > far) break;

    }

    return k;

}

void main(){

    const float cloudTileSize = 50.0;

    vec3 origin = unproject(iMvpMatrix * vec4(ndc, -1.0, 1.0));
    vec3 direction = normalize(unproject(iMvpMatrix * vec4(ndc, 1.0, 1.0)) - origin);
    vec3 world = unproject(iMvpMatrix * vec4(ndc, texture(depthTex, uv).r * 2.0 - 1.0, 1.0));
    float far = distance(origin, world);

    direction = curve(direction, 0.07);
    direction = warp(direction, PerceptionEffectIntensity * 0.03);

    origin.y -= cloudOffset.y;

    vec2 plane = intersect(
        origin.y,
        direction.y,
        vec2(-12.5 - 500 * 0.1, 12.5 + 500.0)
    );

    float near = plane.x;

    if(near < 0.0 || far < near) discard;

    origin += direction * near;
    origin.xz -= cloudOffset.xz;
    origin /= cloudTileSize;
    origin.xz += cloudMapWidth / 2.0;

    far -= near;
    far = min(far, plane.y);
    far = min(far, cloudMapWidth * cloudTileSize / 2.0 - near);
    far /= cloudTileSize;

    vec4 colour = traverse(origin, direction, far);

    if(colour.a <= 0.0) discard;

    colour += gentleNoise() * colour.a * 0.1;
    colour = dither(colour, 255.0);

    drawPixel(colour);

}