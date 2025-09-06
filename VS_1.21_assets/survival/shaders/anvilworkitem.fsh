#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;
#if SSAOLEVEL > 0
in vec4 fragPosition;
in vec4 gnormal;
layout(location = 2) out vec4 outGNormal;
layout(location = 3) out vec4 outGPosition;
#endif

uniform sampler2D tex;
uniform float extraGodray = 0;
uniform float alphaTest = 0.001;


in vec2 uv;
in vec4 color;
in vec4 rgbaFog;
in float fogAmount;
in float glowLevel;
flat in int renderFlags;
flat in vec3 normal;


#include fogandlight.fsh

void main () {
	outColor = texture(tex, uv) * color;
	
#if BLOOM == 0
	outColor.rgb *= 1 + glowLevel;
#endif

	float b = min(1, getBrightnessFromNormal(normal, 1, 0.45) + glowLevel);
	outColor *= vec4(b, b, b, 1);
	
	outColor = applyFogAndShadow(outColor, fogAmount);

	if (outColor.a < alphaTest) discard;
	
#if SSAOLEVEL > 0
	outGPosition = vec4(fragPosition.xyz, fogAmount + glowLevel);
	outGNormal = vec4(gnormal.xyz, 0);
#endif

	outGlow = vec4(glowLevel, extraGodray, 0, outColor.a);
}