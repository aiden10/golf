using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CourseData", menuName = "ScriptableObjects/CourseData", order = 1)]
public class CourseData : ScriptableObject
{
    public List<string> courseNames;
}
