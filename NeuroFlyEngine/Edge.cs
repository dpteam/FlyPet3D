namespace NeuroFlyEngine;

public class Edge
{
	public int Pre;

	public int Post;

	public float Weight;

	public Edge(int pre, int post, float weight)
	{
		Pre = pre;
		Post = post;
		Weight = weight;
	}
}
