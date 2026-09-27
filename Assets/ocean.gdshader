shader_type spatial;
render_mode unshaded;

varying vec3 world_position;

void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    vec2 p = world_position.xz;
    float swell = sin(p.x * 0.23 + p.y * 0.31 + TIME * 0.23);
    float ripple = sin(p.y * 2.5 + sin(p.x * 0.65 + TIME * 0.18) + TIME * 0.42);
    float broken = smoothstep(0.2, 0.85, sin(p.x * 1.2 + p.y * 0.12));
    float line = smoothstep(0.965, 1.0, ripple) * broken;
    vec3 sea = mix(vec3(0.055, 0.24, 0.29), vec3(0.075, 0.32, 0.35), swell * 0.5 + 0.5);
    ALBEDO = sea + line * vec3(0.045, 0.105, 0.10);
}
