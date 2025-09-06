#version 330 core

#include noise2d.ash

const float PI = 3.1415926535897932384626433832795;

in vec4 color;
in vec2 uv;


out vec4 outColor;

uniform int riftIndex;
uniform sampler2D primaryFb;
uniform sampler2D depthTex;
uniform vec2 invFrameSize;
uniform float counter;
uniform float counterSmooth;
uniform float zNear = 0.3;
uniform float zFar = 1500.0;


float linearDepth(float depthSample)
{
    depthSample = 2.0 * depthSample - 1.0;
    float zLinear = 2.0 * zNear / (zFar + zNear - depthSample * (zFar - zNear));
    return zLinear;
}


void main()
{
	float x = gl_FragCoord.x * invFrameSize.x;
	float y = gl_FragCoord.y * invFrameSize.y;
	
	float z1 = linearDepth(texture(depthTex, vec2(x,y)).r); //depth from gbuffer depth, a float32 texture
	float z2 = linearDepth(gl_FragCoord.z);
	
	float aDiff = max(0, z2 - z1) * 100;
	if (aDiff > 2) discard;
	
	float f = length(uv - 0.5) * 2;
	
	float noise = 
		cnoise(vec2(gl_FragCoord.x / 300, gl_FragCoord.y / 300 - counter / 3))  / 50.0
		+ cnoise(vec2(gl_FragCoord.x / 200, gl_FragCoord.y / 200 - counter / 4))  / 75.0
		+ cnoise(vec2(gl_FragCoord.x / 2, gl_FragCoord.y / 2 - counter)) / 200.0
	;
	
	vec4 col = texture(primaryFb, vec2(x,y) + noise);
	
	float angle = mod(atan(uv.y - 0.5, uv.x - 0.5) + (f * counterSmooth/10.0), 2*3.14159);
	
	float seed = riftIndex/10.0 + counterSmooth / 10.0;
	float k = cnoise(vec2(angle * 20, seed))/4 + cnoise(vec2(angle * 5, seed))/4 + cnoise(vec2(seed, angle));
	k = k / 2 + 1;
	
	float b = pow(1.2 - f * k, 1.2);// pow((1.4-f) * k * 1, 1.1);
	
//	b -= pow(f, 4);
	
	col.a = clamp(b, 0, 1);
	col.a = pow(col.a, 4);
	col.a = clamp(col.a - aDiff, 0, 1);
	
	vec3 rust = vec3(
			(col.r * 0.393) + (col.g * 0.769) + (col.b * 0.189),
			(col.r * 0.349) + (col.g * 0.686) + (col.b * 0.168),
			(col.r * 0.272) + (col.g * 0.534) + (col.b * 0.131)
	);	
	float gdiff = min(col.g, 0.1);
	float bdiff = min(col.b, 0.1);
	rust.g -= gdiff;
	rust.b -= bdiff;
	rust.r += gdiff + bdiff;
	col.rgb = mix(col.rgb, rust, 1);
	col.rgb /= 3.5;
	outColor=col;
}