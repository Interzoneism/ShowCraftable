#version 330 core

const float PI = 3.1415926535897932384626433832795;

in vec2 uv;
in vec4 rgbaFog;
in float fogAmount;


layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;

uniform int riftIndex;

uniform vec2 invFrameSize;
uniform float alpha = 1.0;

#include fogandlight.fsh

void main()
{
	outColor = vec4(0.1, 0.05, 0, alpha);
	outColor = applyFogAndShadow(outColor, 0.7);
	
	outGlow = vec4(0, 0, 0, outColor.a);
}