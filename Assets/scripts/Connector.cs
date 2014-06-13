/* Connector.cs
 * Copyright Eddie Cameron 2014
 * ----------------------------
 *
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Connector : MonoBehaviour {
    [Tooltip( "If a doorframe (no distance between each end), leave one of these null")]
    public Transform end1, end2;
}