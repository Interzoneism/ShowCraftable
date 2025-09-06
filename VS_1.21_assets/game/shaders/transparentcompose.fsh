#version 330 core

uniform sampler2D accumulation;
uniform sampler2D revealage;
uniform sampler2D inGlow;

in vec2 v_texcoord;

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;
#if SSAOLEVEL > 0
layout(location = 2) out vec4 outGNormal;
layout(location = 3) out vec4 outGPosition;
#endif


void main(void)
{
	float reveal = max(0, texture(revealage, v_texcoord).r);
	
	if (reveal >= 1.0) {
		discard;
	}
	
	vec4 accum = texture(accumulation, v_texcoord);
	

	// Many particles overlapping eachother made accum.a huge, caused it to render completely black
	//vec3 avgColor = accum.rgb / max(accum.a, 0.00001);
	
	// Clamping accum.a to 15000 seems to do a really good job at fixing it and making fire look really fiery \o/
	// 65000 still seems ok and doesn't make a lot of overlaid dust particles all white
	vec3 avgColor = accum.rgb / clamp(accum.a, 0.00001, 65000);
	
	outColor = vec4(avgColor, 1 - reveal);
	
	outGlow = texture(inGlow, v_texcoord);
}