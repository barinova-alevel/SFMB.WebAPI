using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFMB.BL.Dtos;
using SFMB.DAL.Entities;
using OperationType = SFMB.DAL.Entities.OperationType;
using SFMB.BL.Services.Interfaces;
using SFMB.DAL.Repositories.Interfaces;

namespace WebApiForAz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OperationTypesController : ControllerBase
    {
        private readonly SFMB.BL.Services.Interfaces.IOperationTypeService _operationTypeService;
        private readonly SFMB.BL.Services.Interfaces.IOperationService _operationService;
        private readonly IOperationTypeRepository _operationTypeRepository;

        public OperationTypesController(IOperationTypeService operationTypeService, IOperationService operationService, IOperationTypeRepository operationTypeRepository)
        {
            _operationTypeService = operationTypeService;
            _operationService = operationService;
            _operationTypeRepository = operationTypeRepository;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        // GET: api/OperationTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OperationType>>> GetOperationTypes()
        {
            var userId = GetCurrentUserId();
            IEnumerable<OperationType> operationTypes;

            if (IsAdmin())
            {
                operationTypes = await _operationTypeRepository.GetAllAsync();
            }
            else
            {
                operationTypes = await _operationTypeRepository.GetAllByUserAsync(userId);
            }

            if (operationTypes == null)
            {
                return Ok(new List<OperationType>());
            }

            return Ok(operationTypes.ToList());
        }

        // GET: api/OperationTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OperationType>> GetOperationType(int id)
        {
            var userId = GetCurrentUserId();
            OperationType? operationType;

            if (IsAdmin())
            {
                operationType = await _operationTypeRepository.GetByIdAsync(id);
            }
            else
            {
                operationType = await _operationTypeRepository.GetByIdAndUserAsync(id, userId);
            }

            if (operationType == null)
            {
                return NotFound();
            }

            return Ok(operationType);
        }

        // PUT: api/OperationTypes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOperationType(int id, OperationType operationType)
        {
            if (id != operationType.OperationTypeId)
            {
                return BadRequest("Operation Type Id mismatch.");
            }

            var userId = GetCurrentUserId();
            OperationType? existingOperationType;

            if (IsAdmin())
            {
                existingOperationType = await _operationTypeRepository.GetByIdAsync(id);
            }
            else
            {
                existingOperationType = await _operationTypeRepository.GetByIdAndUserAsync(id, userId);
            }

            if (existingOperationType == null)
            {
                return NotFound($"Operation Type with id {id} not found or you don't have permission.");
            }

            // Ensure UserId doesn't change
            operationType.UserId = existingOperationType.UserId;

            var operationTypeDto = new OperationTypeDto
            {
                OperationTypeId = operationType.OperationTypeId,
                Name = operationType.Name,
                Description = operationType.Description,
                IsIncome = operationType.IsIncome
            };

            var updatedOperationType = await _operationTypeService.UpdateAsync(id, operationTypeDto);

            if (updatedOperationType == null)
            {
                return NotFound($"Operation Type with id {id} not found.");
            }

            return Ok(updatedOperationType);
        }

        // POST: api/OperationTypes
        [HttpPost]
        public async Task<ActionResult<OperationType>> PostOperationType(OperationType operationType)
        {
            var userId = GetCurrentUserId();
            
            // Create entity with UserId
            var operationTypeEntity = new OperationType
            {
                Name = operationType.Name,
                Description = operationType.Description,
                IsIncome = operationType.IsIncome,
                UserId = userId
            };
            
            var createdOperationType = await _operationTypeRepository.AddAsync(operationTypeEntity);

            if (createdOperationType == null)
            {
                return BadRequest("Failed to create Operation Type.");
            }

            if (operationType.Operations != null && operationType.Operations.Any())
            {
                foreach (var op in operationType.Operations)
                {
                    op.OperationTypeId = createdOperationType.OperationTypeId;
                    op.UserId = userId;

                    var operationDto = new OperationDto
                    {
                        Date = op.Date,
                        Amount = op.Amount,
                        Note = op.Note,
                        OperationTypeId = op.OperationTypeId
                    };

                    var createdOperation = await _operationService.CreateAsync(operationDto);

                    if (createdOperation == null)
                    {
                        return StatusCode(500, "Failed to create one or more associated operations.");
                    }
                }
            }

            //Read the created OperationType
            var fullOperationType = await _operationTypeRepository.GetByIdAsync(createdOperationType.OperationTypeId);

            if (fullOperationType == null)
            {
                return NotFound($"Operation Type with id {createdOperationType.OperationTypeId} not found after creation.");
            }

            //Return the created resource
            return CreatedAtAction("GetOperationType", new { id = fullOperationType.OperationTypeId }, fullOperationType);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOperationType(int id)
        {
            var userId = GetCurrentUserId();
            OperationType? operationType;

            if (IsAdmin())
            {
                operationType = await _operationTypeRepository.GetByIdAsync(id);
            }
            else
            {
                operationType = await _operationTypeRepository.GetByIdAndUserAsync(id, userId);
            }

            if (operationType == null)
            {
                return NotFound($"Operation Type with id {id} not found or you don't have permission.");
            }

            await _operationTypeService.DeleteAsync(id);
            return NoContent();
        }
    }
}
