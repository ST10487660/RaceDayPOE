using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Age / distance categories (e.g. Under 20, Senior, 10 km, 21 km).</summary>
[Route("api/v1/categories")]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists all categories (Public).</summary>
    /// <response code="200">All categories, ordered by name.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();
        return Ok(categories.Select(EventMapper.ToCategoryDto));
    }

    /// <summary>Creates a category (Organiser only).</summary>
    /// <remarks>Send { categoryName, description }. Category names must be unique.</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    /// <response code="409">A category with this name already exists.</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Organiser)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryRequest request)
    {
        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.CategoryName.ToLower() == name.ToLower()))
            return Error(409, "A category with this name already exists.");

        var category = new Category { CategoryName = name, Description = request.Description?.Trim() };
        _db.Categories.Add(category);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Error(409, "A category with this name already exists.");
        }

        return Created($"/api/v1/categories/{category.CategoryId}", EventMapper.ToCategoryDto(category));
    }

    /// <summary>Updates a category (Organiser only).</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Another category already has this name.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] CategoryRequest request)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return Error(404, "Category not found.");

        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.CategoryName.ToLower() == name.ToLower() && c.CategoryId != id))
            return Error(409, "A category with this name already exists.");

        category.CategoryName = name;
        category.Description = request.Description?.Trim();
        await _db.SaveChangesAsync();
        return Ok(EventMapper.ToCategoryDto(category));
    }

    /// <summary>Deletes a category that is not used by any event (Organiser only).</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category is used by one or more events.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return Error(404, "Category not found.");

        if (await _db.EventCategories.AnyAsync(ec => ec.CategoryId == id))
            return Error(409, "This category is in use by one or more events and cannot be deleted.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
