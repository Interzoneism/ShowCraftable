#version 330 core

//layout(location=0) in vec2 position;

uniform vec2 invFrameSizeIn;
uniform vec3 sunPosScreenIn;
uniform vec3 sunPos3dIn;
uniform vec3 playerViewVector;

out vec2 texCoord;
out vec2 invFrameSize;
flat out float godrayIntensity;

void main(void)
{
	// https://rauwendaal.net/2014/06/14/rendering-a-screen-covering-triangle-in-opengl/
	float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    gl_Position = vec4(x, y, 0, 1);
    texCoord = vec2((x+1.0) * 0.5, (y + 1.0) * 0.5);

/*    gl_Position = vec4(position, 0, 1);
    texCoord = (position+1.0) / 2.0;*/
	invFrameSize = invFrameSizeIn;
	
	// Copied from godrays.vsh, should be a #include
	float sunPlrAngle = 0.5 + 0.25 * (dot(sunPos3dIn, playerViewVector) + 1);
	float dawnDuskMul = max(1, 1.75 * (1 - 6*abs(sunPos3dIn.y - 0.22)));
	float nightFade = max(0, -2*sunPos3dIn.y + 0.4);
	
	// Intensity is determined by how directly the player is looking at the sun
	godrayIntensity = max(0, sunPlrAngle * dawnDuskMul - nightFade) / 2;
}