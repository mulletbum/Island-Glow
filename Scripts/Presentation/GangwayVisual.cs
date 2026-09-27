using Godot;
using IslandGlow.Core;

namespace IslandGlow.Presentation;

/// <summary>One reusable, world-aligned ramp. The authority supplies both attachment points and deployment.</summary>
public partial class GangwayVisual : Node3D
{
    private Node3D? _deck;
    private Vector3 _start, _end;

    public void Update(Vector3 start, Vector3 end, bool show)
    {
        Visible = show;
        if (!show) return;
        // World-origin shifts only move this root; planks are rebuilt only when the berth span changes.
        Position = start;
        Vector3 span = end - start;
        if (_deck != null && _start.DistanceSquaredTo(Vector3.Zero) < 0.0001f && _end.DistanceSquaredTo(span) < 0.0001f) return;
        _start = Vector3.Zero; _end = span;
        if (_deck != null) { RemoveChild(_deck); _deck.QueueFree(); }
        _deck = new Node3D { Name = "GangwayPlanks" }; AddChild(_deck);
        if (span.LengthSquared() < 0.01f) return;
        float width = (float)HarbourAccess.HalfWidth;
        Vector3 along = span.Normalized();
        Vector3 across = Vector3.Up.Cross(along).Normalized();
        Vector3 normal = along.Cross(across).Normalized();
        var basis = new Basis(across, normal, along);
        int boards = Mathf.CeilToInt(span.Length() / 0.42f);
        float length = span.Length() / boards;
        for (int i = 0; i < boards; i++)
        {
            var plank = Art.Box(_deck, span * ((i + 0.5f) / boards) - normal * 0.065f,
                new(width * 2, 0.13f, length - 0.014f), i % 3 == 0 ? "c7ab77" : "b39867");
            plank.Basis = basis;
        }
        foreach (float side in new[] { -1f, 1f })
        {
            Vector3 edge = across * (width - 0.06f) * side;
            Art.Beam(_deck, edge - normal * 0.18f, span + edge - normal * 0.18f, 0.11f, "6d5138");
            int posts = Mathf.CeilToInt(span.Length() / 3.5f);
            for (int post = 0; post <= posts; post++)
            {
                Vector3 foot = span * (post / (float)posts) + edge;
                Art.Beam(_deck, foot, foot + Vector3.Up * 0.94f, 0.06f, "71573a");
                if (post > 0)
                    Art.Rope(_deck, span * ((post - 1) / (float)posts) + edge + Vector3.Up * 0.94f,
                        foot + Vector3.Up * 0.94f, 0.08f, 0.03f, "d6c299");
            }
        }
        // Brass-coloured threshold strips make the small change in height legible.
        foreach (float t in new[] { 0.025f, 0.975f })
        {
            var threshold = Art.Box(_deck, span * t + normal * 0.008f, new(width * 2, 0.025f, 0.07f), "d1ad63");
            threshold.Basis = basis;
        }
    }
}
