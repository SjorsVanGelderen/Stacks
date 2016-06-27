/*
	Just rotates the object's transform over time
*/

using UnityEngine;
using System.Collections;

public class RotationController : MonoBehaviour
{
	void Update()
	{
		transform.Rotate(new Vector3(150.0f, 100.0f, 50.0f) * Time.deltaTime);
	}
}
