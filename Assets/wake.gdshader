shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;
void fragment() {
    float spread = 0.12 + UV.y * 0.34;
    float edge = 1.0 - smoothstep(0.005, 0.048, abs(abs(UV.x - 0.5) - spread));
    float inside = (1.0 - smoothstep(spread * 0.6, spread, abs(UV.x - 0.5))) * 0.3;
    float ripple = 0.7 + 0.3 * sin(UV.y * 70.0 - TIME * 3.5 + UV.x * 8.0);
    ALBEDO = vec3(0.55, 0.77, 0.70);
    ALPHA = (edge + inside) * pow(1.0 - UV.y, 1.5) * ripple * 0.32;
}
