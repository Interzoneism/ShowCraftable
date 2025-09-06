#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

in vec4 color;
in float glowLevel;
in vec4 rgbaFog;

layout(location = 0) out vec4 outAccu;
layout(location = 1) out vec4 outReveal;
layout(location = 2) out vec4 outGlow;

uniform sampler2D particleTex;

#include fogandlight.fsh


void drawPixel(vec4 color) {
	float weight = color.a * clamp(0.03 / (1e-5 + pow(gl_FragCoord.z / 200, 4.0)), 1e-2, 3e3);
	
    // RGBA32F texture (accumulation)
    outAccu = vec4(color.rgb * color.a, color.a) * weight;
	
    // R32F texture (revealage)
    // Make sure to use the red channel (and GL_RED target in your texture)
    outReveal.r = color.a;

    outGlow = vec4(glowLevel, 0, 0, color.a);
}

void main()
{
    drawPixel(color);
}

