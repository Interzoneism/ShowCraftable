#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

in vec4 color;
in vec2 uv;
in vec4 rgbaFog;
in vec3 vexPos;
in float fogAmount;
in float glowLevel;
in float extraWeight;


layout(location = 0) out vec4 outAccu;
layout(location = 1) out vec4 outReveal;
layout(location = 2) out vec4 outGlow;

uniform sampler2D particleTex;

#include fogandlight.fsh
#include underwatereffects.fsh

void drawPixel(vec4 color) {
	float weight = color.a * clamp(0.03 / (1e-5 + pow(gl_FragCoord.z / 200, 4.0)), 1e-2, 1e3);
	
    // RGBA32F texture (accumulation)
    outAccu = vec4(color.rgb * color.a, color.a) * (weight * extraWeight);

    // R32F texture (revealage)
    // Make sure to use the red channel (and GL_RED target in your texture)
    outReveal.r = color.a; 	
	
    outGlow = vec4(glowLevel, 0, 0, color.a);
}


void main()
{
	vec4 outColor;
	
	float murkiness=getUnderwaterMurkiness();
	if (murkiness > 0) {
		outColor = applyFogAndShadow(color, 0);
		outColor.rgb = applyUnderwaterEffects(outColor.rgb, murkiness);	
	} else {	
		outColor = applyFogAndShadow(color, fogAmount);
	}

	vec2 uvdist = vec2(
		max(max(0, 0.1 - uv.x), max(0, uv.x - 0.9)),
		max(max(0, 0.1 - uv.y), max(0, uv.y - 0.9))
	);
	
	outColor.a *= 1 - length(uvdist)*10;	
	drawPixel(outColor);
}

