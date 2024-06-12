using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BorrowRecordsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BorrowRecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Route("borrow")]
        public async Task<IActionResult> BorrowBook([FromBody] BorrowRecord borrowRecord)
        {
            var book = await _context.Books.FindAsync(borrowRecord.BookId);
            if (book == null || !book.IsAvailable)
            {
                return BadRequest("Book is not available.");
            }

            var user = await _context.Users.FindAsync(borrowRecord.UserId);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            book.IsAvailable = false;
            borrowRecord.BorrowedAt = DateTime.UtcNow;

            _context.BorrowRecords.Add(borrowRecord);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBorrowRecordById), new { id = borrowRecord.Id }, borrowRecord);
        }

        [HttpPost]
        [Route("return")]
        public async Task<IActionResult> ReturnBook([FromBody] BorrowRecord borrowRecord)
        {
            var record = await _context.BorrowRecords
                .Where(r => r.BookId == borrowRecord.BookId && r.UserId == borrowRecord.UserId && r.ReturnedAt == null)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return BadRequest("Borrow record not found.");
            }

            record.ReturnedAt = DateTime.UtcNow;

            var book = await _context.Books.FindAsync(borrowRecord.BookId);
            if (book != null)
            {
                book.IsAvailable = true;
            }

            await _context.SaveChangesAsync();
            return Ok(record);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBorrowRecords()
        {
            var records = await _context.BorrowRecords.Include(br => br.Book).Include(br => br.User).ToListAsync();
            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBorrowRecordById(int id)
        {
            var record = await _context.BorrowRecords.Include(br => br.Book).Include(br => br.User).FirstOrDefaultAsync(br => br.Id == id);
            if (record == null)
            {
                return NotFound();
            }
            return Ok(record);
        }
    }
}
