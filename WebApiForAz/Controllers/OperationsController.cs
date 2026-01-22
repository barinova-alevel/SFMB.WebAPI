using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFMB.BL.Dtos;
using SFMB.BL.Services.Interfaces;
using SFMB.DAL.Entities;
using SFMB.DAL.Repositories.Interfaces;

namespace WebApiForAz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OperationsController : ControllerBase
    {
        private readonly IOperationService _operationService;
        private readonly IOperationRepository _operationRepository;

        public OperationsController(IOperationService operationService, IOperationRepository operationRepository)
        {
            _operationService = operationService;
            _operationRepository = operationRepository;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        // GET: api/Operations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Operation>>> GetAllOperations()
        {
            var userId = GetCurrentUserId();
            IEnumerable<Operation> operations;

            if (IsAdmin())
            {
                operations = await _operationRepository.GetAllAsync();
            }
            else
            {
                operations = await _operationRepository.GetAllByUserAsync(userId);
            }

            if (operations == null)
            {
                return Ok(new List<Operation>());
            }

            return Ok(operations.ToList());
        }

        // GET: api/Operations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Operation>> GetOperation(int id)
        {
            var userId = GetCurrentUserId();
            Operation? operation;

            if (IsAdmin())
            {
                operation = await _operationRepository.GetByIdAsync(id);
            }
            else
            {
                operation = await _operationRepository.GetByIdAndUserAsync(id, userId);
            }

            if (operation == null)
            {
                return NotFound($"Operation with id {id} not found");
            }

            return Ok(operation);
        }

        // PUT: api/Operations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOperation(int id, SFMB.DAL.Entities.Operation operation)
        {
            if (id != operation.OperationId)
            {
                return BadRequest("Operation Id mismatch.");
            }

            var userId = GetCurrentUserId();
            Operation? existingOperation;

            if (IsAdmin())
            {
                existingOperation = await _operationRepository.GetByIdAsync(id);
            }
            else
            {
                existingOperation = await _operationRepository.GetByIdAndUserAsync(id, userId);
            }

            if (existingOperation == null)
            {
                return NotFound($"Operation with id {id} not found or you don't have permission.");
            }

            // Ensure UserId doesn't change
            operation.UserId = existingOperation.UserId;

            var operationDto = new OperationDto
            {
                OperationId = operation.OperationId,
                Date = operation.Date,
                Amount = operation.Amount,
                Note = operation.Note,
                OperationTypeId = operation.OperationTypeId
            };

            var updatedDto = await _operationService.UpdateAsync(id, operationDto);

            if (updatedDto == null)
            {
                return NotFound($"Operation with id {id} not found.");
            }

            return Ok(updatedDto);
        }

        // POST: api/Operations
        [HttpPost]
        public async Task<ActionResult<Operation>> PostOperation(Operation operation)
        {
            var validationResult = ValidateOperation(operation);
            if (validationResult is not null)
                return validationResult;

            // Set the UserId from the current user
            var userId = GetCurrentUserId();
            
            // Create the operation entity with UserId
            var operationEntity = new Operation
            {
                Date = operation.Date,
                Amount = operation.Amount,
                Note = operation.Note,
                OperationTypeId = operation.OperationTypeId,
                UserId = userId
            };
            
            var createdOperation = await _operationRepository.AddAsync(operationEntity);

            if (createdOperation == null)
            {
                return BadRequest("Failed to create Operation.");
            }

            var checkNewOperation = await _operationRepository.GetByIdAsync(createdOperation.OperationId);

            if (checkNewOperation == null)
            {
                return BadRequest($"Operation with id {createdOperation.OperationId} not found after creation.");
            }

            return CreatedAtAction(nameof(GetOperation), new { id = createdOperation.OperationId }, createdOperation);
        }

        // DELETE: api/Operations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOperation(int id)
        {
            var userId = GetCurrentUserId();
            Operation? operation;

            if (IsAdmin())
            {
                operation = await _operationRepository.GetByIdAsync(id);
            }
            else
            {
                operation = await _operationRepository.GetByIdAndUserAsync(id, userId);
            }

            if (operation == null)
            {
                return NotFound($"Operation with id {id} not found or you don't have permission.");
            }

            var isDeleted = await _operationService.DeleteAsync(id);

            if (!isDeleted)
            {
                return BadRequest($"Failed to delete Operation with id {id}.");
            }

            return NoContent();
        }

        private ActionResult? ValidateOperation(Operation? operation)
        {
            if (operation == null)
                return BadRequest("Operation data is required.");

            if (operation.Amount == default)
                return BadRequest("Amount is required.");

            if (operation.OperationTypeId == default)
                return BadRequest("OperationTypeId is required.");

            return null;
        }
    }
}
