using UnityEngine;
using System.Collections;
using System.Collections.Generic;
	// public void Shuffle(List<int> list)
	// {
	// 	int n = list.Count;
	// 	while (n > 1) {
	// 		n--;
	// 		int k = Random.Range(0,n + 1);
	// 		int value = list[k];
	// 		list[k] = list[n];
	// 		list[n] = value;
	// 	}
	// }

public static class TensionExtensions
{
	public static void Shuffle<T>( this IList<T> toShuffle ){
		int n = toShuffle.Count;
		while (n>1){
			n--;
			int k = Random.Range(0, n+1);
			T val = toShuffle[k];
			toShuffle[k] = toShuffle[n];
			toShuffle[n] = val;
		}
	}

	#region Vector2 extensions
	public static Vector2 Rotate(this Vector2 v, float a){
		float px = v.x*Mathf.Cos(a) - v.y*Mathf.Sin(a);
		float py = v.x*Mathf.Sin(a) + v.y*Mathf.Cos(a);
		return new Vector2(px, py);
	}
	public static float Angle(this Vector2 v){
		return Mathf.Atan2(v.x, v.y)*180.0f/Mathf.PI;
	}
	public static Vector2 Variation(this Vector2 v, float amount){
		return new Vector2(v.x+(Random.value-0.5f)*amount, v.y+(Random.value-0.5f)*amount);	
	}
	#endregion

	#region Vector3 extensions
	public static Vector3 Variation(this Vector3 v, Vector3 amount){
		return new Vector3(v.x+(Random.value*amount.x)-amount.x*0.5f, v.y+(Random.value*amount.y)-amount.y*0.5f, v.z+(Random.value-0.5f)*amount.z);
	}
	public static Vector3 Variation(this Vector3 v, float amount){
		return new Vector3(v.x+(Random.value-0.5f)*amount, v.y+(Random.value-0.5f)*amount, v.z+(Random.value-0.5f)*amount);
	}
	#endregion
	#region Color extensions
	public static Color Alpha(this Color c, float newAlpha){
		return new Color(c.r, c.g, c.b, newAlpha);
	}
	#endregion
}
