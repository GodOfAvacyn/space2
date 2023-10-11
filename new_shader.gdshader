shader_type spatial;
render_mode unshaded, cull_front;

void vertex() {
	VERTEX = VERTEX + NORMAL*0.02;
}

void fragment() {
	ALBEDO = vec3(0);
}
