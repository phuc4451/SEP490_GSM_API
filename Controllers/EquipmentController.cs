﻿using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Alpha_API.Models;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]")]
[ApiController]
//[Authorize(Policy = "AdminOnly")]
[Authorize(Roles = "admin")]
public class EquipmentController : ControllerBase
{
    private readonly FirebaseClient _firebaseClient;

    public EquipmentController()
    {
        _firebaseClient = new FirebaseClient("https://sgm-management-c98cd-default-rtdb.firebaseio.com/");
    }

    // GET: api/course
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipments()
    {
        var equipments = await _firebaseClient
            .Child("Equipment")
            .OnceAsync<Equipment>();

        var equipmentList = new List<Equipment>();
        foreach (var equipment in equipments)
        {
            var equipmentObject = equipment.Object;
            equipmentObject.EquipmentId = equipment.Key; // Set CourseId from Firebase key
            equipmentList.Add(equipmentObject);
        }

        return Ok(equipmentList);
    }

    // POST: api/course
    [HttpPost]
    public async Task<ActionResult<Equipment>> PostCourse([FromBody] Equipment equipment)
    {
        if (equipment == null)
        {
            return BadRequest();
        }

        // Add course to Firebase and use auto-generated key
        var result = await _firebaseClient
            .Child("Equipment")
            .PostAsync(new
            {
                equipment.EquipmentName,
                equipment.EquipmentCode,
                equipment.EquipmentImportPrice,
                equipment.EquipmentBrand,
                equipment.EquipmentQuantity,
                equipment.EquipmentCategoryId,
                equipment.EquipmentStatusId,
                equipment.EquipmentConditionId,
                equipment.TrainingRoomId,
                equipment.EquipmentManufactured,
                equipment.EquipmentSize,
                equipment.EquipmentWeightStack,
                equipment.EquipmentMaterial// Add CourseDuration field here
            });

        // Set the CourseId to Firebase's auto-generated key
        equipment.EquipmentId = result.Key;
        return CreatedAtAction(nameof(GetEquipmentById), new { id = equipment.EquipmentId }, equipment);
    }

    // GET: api/equipment/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Equipment>> GetEquipmentById(string id)
    {
        var equipment = await _firebaseClient
            .Child("Equipment")
            .Child(id.ToString())
            .OnceSingleAsync<Equipment>();

        if (equipment == null)
        {
            return NotFound();
        }

        equipment.EquipmentId = id; // Set CourseId based on the requested ID
        return Ok(equipment);
    }

    //// PUT: api/course/{id}
    //[HttpPut("{id}")]
    //public async Task<ActionResult> PutCourse(string id, [FromBody] Course course)
    //{
    //    if (id != course.CourseId)
    //    {
    //        return BadRequest();
    //    }

    //    await _firebaseClient
    //        .Child("Courses")
    //        .Child(id.ToString())
    //        .PutAsync(course);

    //    return NoContent(); // 204 No Content
    //}

    //// PATCH: api/course/{id}
    //[HttpPatch("{id}")]
    //public async Task<ActionResult> PatchCourse(string id, [FromBody] Course course)
    //{
    //    var existingCourse = await _firebaseClient
    //        .Child("Courses")
    //        .Child(id.ToString())
    //        .OnceSingleAsync<Course>();

    //    if (existingCourse == null)
    //    {
    //        return NotFound();
    //    }

    //    // Update only the modified fields
    //    if (!string.IsNullOrEmpty(course.CourseName))
    //    {
    //        existingCourse.CourseName = course.CourseName;
    //    }
    //    if (!string.IsNullOrEmpty(course.CourseContent))
    //    {
    //        existingCourse.CourseContent = course.CourseContent;
    //    }
    //    if (course.CoursePrice > 0)
    //    {
    //        existingCourse.CoursePrice = course.CoursePrice;
    //    }
    //    //if (course.CourseDuration > 0)
    //    //{
    //    //	existingCourse.CourseDuration = course.CourseDuration;
    //    //}

    //    await _firebaseClient
    //        .Child("Courses")
    //        .Child(id.ToString())
    //        .PutAsync(existingCourse);

    //    return NoContent(); // 204 No Content
    //}

    //// DELETE: api/course/{id}
    //[HttpDelete("{id}")]
    //public async Task<ActionResult> DeleteCourse(string id)
    //{
    //    var existingCourse = await _firebaseClient
    //        .Child("Courses")
    //        .Child(id.ToString())
    //        .OnceSingleAsync<Course>();

    //    if (existingCourse == null)
    //    {
    //        return NotFound();
    //    }

    //    await _firebaseClient
    //        .Child("Courses")
    //        .Child(id.ToString())
    //        .DeleteAsync();

    //    return NoContent(); // 204 No Content
    //}
}