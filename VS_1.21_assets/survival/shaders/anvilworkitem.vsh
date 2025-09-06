#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
layout(location = 2) in vec4 colorIn;
layout(location = 3) in int flags;

layout(location = 4) in float glowSub;

uniform vec4 rgbaTint;
uniform vec3 rgbaAmbientIn;
uniform vec4 rgbaLightIn;
uniform vec4 rgbaGlowIn;
uniform vec4 rgbaFogIn;
uniform int extraGlow;
uniform float fogMinIn;
uniform float fogDensityIn;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

uniform int dontWarpVertices;
uniform int addRenderFlags;
uniform float extraZOffset;

out vec2 uv;
out vec4 color;
out vec4 rgbaFog;
out float fogAmount;

flat out int renderFlags;
flat out vec3 normal;
#if SSAOLEVEL > 0
out vec4 fragPosition;
out vec4 gnormal;
#endif

#include vertexflagbits.ash
#include shadowcoords.vsh
#include fogandlight.vsh
#include vertexwarp.vsh

void main(void)
{
	vec4 worldPos = modelMatrix * vec4(vertexPositionIn, 1.0);
	
	if (dontWarpVertices == 0) {
		worldPos = applyVertexWarping(flags | addRenderFlags, worldPos);
		worldPos = applyGlobalWarping(worldPos);
	}
	
	vec4 camPos = viewMatrix * worldPos;
	
	uv = uvIn;
	int glow = clamp(extraGlow + (flags & 0xff) - int(glowSub * 255), 0, 255);
	renderFlags = glow | (flags & ~0xff);

	
	vec4 rgbaGlow = vec4(0,0,0,1);
	rgbaGlow.rgb = rgbaGlowIn.rgb * max(vec3(0), (1 - vec3(3*glowSub)));
	vec4 rgbaLight = vec4(rgbaGlow.rgb + rgbaLightIn.rgb, rgbaLightIn.a);
	
	color = rgbaTint * applyLight(rgbaAmbientIn, rgbaLight * colorIn, renderFlags, camPos);	
	color.rgb *= 1 - 0.5 * glowSub;
	color.rgb = mix(color.rgb, rgbaGlow.rgb, max(0, glowLevel - glowSub));
	
	glowLevel -= glowSub;
	
	rgbaFog = rgbaFogIn;	
	gl_Position = projectionMatrix * camPos;
	calcShadowMapCoords(viewMatrix, worldPos);
	
	fogAmount = getFogLevel(worldPos, fogMinIn, fogDensityIn);
	
	gl_Position.w += extraZOffset;
	
	normal = unpackNormal(flags >> 15);
	normal = normalize((modelMatrix * vec4(normal.x, normal.y, normal.z, 0)).xyz);
	
	#if SSAOLEVEL > 0
		fragPosition = camPos;
		gnormal = viewMatrix * vec4(normal, 0);
	#endif
}