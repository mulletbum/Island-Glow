shader_type spatial;
render_mode unshaded;
uniform float chart = 0.0;
uniform float daylight = 1.0;
uniform vec2 world_offset = vec2(0.0);
uniform bool cutaway = false;
uniform vec2 cutaway_center = vec2(0.0);
uniform float cutaway_heading = 0.0;
varying vec2 ocean_position;
void vertex() { ocean_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xz + world_offset; }
void fragment() {
    vec2 p = ocean_position;
    if (cutaway) {
        vec2 d = p - cutaway_center;
        float c = cos(cutaway_heading), s = sin(cutaway_heading);
        vec2 deck = vec2(d.x * c - d.y * s, d.x * s + d.y * c);
        float width = deck.y < -12.0 ? mix(0.0, 4.2, (deck.y + 17.0) / 5.0) : deck.y < -7.0 ? mix(4.2, 5.8, (deck.y + 12.0) / 5.0) : deck.y <= 8.0 ? 5.8 : mix(5.8, 4.8, (deck.y - 8.0) / 5.0);
        if (deck.y > -17.0 && deck.y < 13.0 && abs(deck.x) < width + 0.04) discard;
    }
    float swell = sin(p.x * 0.09 + p.y * 0.14 + TIME * 0.22);
    float wave = sin(p.y * 1.25 + sin(p.x * 0.35 + TIME * 0.28) * 1.2 + TIME * 0.5);
    float footprint = length(fwidth(p));
    float sharpness = 1.0 - smoothstep(0.10, 0.85, footprint);
    float foam = smoothstep(0.96, 1.0, wave) * smoothstep(0.65, 0.96, sin(p.x * 0.8 + p.y * 0.2)) * sharpness;
    float broad_swell = sin(p.x * 0.012 + p.y * 0.019 + TIME * 0.12);
    vec3 sea = mix(vec3(0.044, 0.22, 0.25), vec3(0.068, 0.28, 0.29), mix(broad_swell, swell, sharpness * 0.5) * 0.5 + 0.5);
    sea += foam * vec3(0.065, 0.10, 0.085);
    float grid = 1.0 - smoothstep(0.0, 0.02, min(abs(fract(p.x / 1000.0) - 0.5), abs(fract(p.y / 1000.0) - 0.5)));
    vec3 chart_sea = mix(vec3(0.13, 0.29, 0.32), vec3(0.18, 0.34, 0.36), grid * 0.3);
    ALBEDO = mix(sea * mix(0.40, 1.0, daylight), chart_sea, chart);
}
