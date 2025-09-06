uniform sampler2D liquidDepth;
uniform float cameraUnderwater;
uniform vec2 frameSize;
uniform vec4 waterMurkColor;

float getSkyMurkiness() {
	if (cameraUnderwater > 0.7) {
		return 0.0;
	}
	
	// Smoother ocean edge
	float ldepth1 = linearDepth(texture(liquidDepth, gl_FragCoord.xy/frameSize.xy).r);
	float ldepth2 = linearDepth(texture(liquidDepth, (gl_FragCoord.xy + vec2(0,3))/frameSize.xy).r);
	float ldepth3 = linearDepth(texture(liquidDepth, (gl_FragCoord.xy + vec2(0,6))/frameSize.xy).r);
	
	return 1-(ldepth1+ldepth2+ldepth3)/3.0;
}

float getUnderwaterMurkiness() {
	if (cameraUnderwater > 0.7) {
		return 0.0;
	}

	// We render the liquid depth z-buffer at 1/4th the resolution. This seems to cause black lines near the shore line
	// when there is strong fog. Probably because of the harsh transition of fog level above vs below water
	// Seems to be fixable by either doing full resolution render or doing a second sample. Pretty sure 2 texture reads on a tiny texture is way faster
	// so lets do that.
	float ldepth = linearDepth(
		max(
			texture(liquidDepth, gl_FragCoord.xy/frameSize.xy).r,
			texture(liquidDepth, (gl_FragCoord.xy + vec2(0,3))/frameSize.xy).r
		)
	);
	
	float fdepth = linearDepth(gl_FragCoord.z);
	return clamp(max(0, fdepth - ldepth)*350.0, 0.0, 1.0);
}

vec3 applyUnderwaterEffects(vec3 color, float murkiness) {
	return mix(color.rgb, waterMurkColor.rgb * 0.4, murkiness);
}
