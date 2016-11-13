﻿

layout(location = 0) out float coc_all;
layout(location = 1) out float coc_foreground;
layout(location = 2) out float coc_foreground_2;


in vec2 v_TexCoord;

uniform sampler2D sampler0;		// Depth


uniform float PPM;
uniform float focus_length;
uniform float fStop;

uniform float max_blur;


layout (std430, binding=0) buffer fd
{
	vec2 focus_distance[];
};



float calculateCOC(float depth, float current_focus_distance)
{
	float ms = focus_length / (current_focus_distance - focus_length);
    float blur_coef = (focus_length * ms) / fStop;

	float xd = abs(depth - current_focus_distance);
	float xdd = (depth < current_focus_distance) ? (current_focus_distance - xd) : (current_focus_distance + xd);
	float b = blur_coef * (xd / xdd);

	return b * PPM;
}



void main()
{

	float depth = texture(sampler0, v_TexCoord).w;
	float coc = calculateCOC(depth, focus_distance[0].y);



	coc_all = coc;
	coc_foreground = coc;
	

	// Black out scene behind the current focus distance giving you the foreground only
	if(depth >= focus_distance[0].y)
	{
		coc_foreground = 0.0;
	}

	coc_foreground_2 = coc_foreground;
}

