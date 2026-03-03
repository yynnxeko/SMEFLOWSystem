using SharedKernel.DTOs;
using SMEFLOWSystem.Application.DTOs.AttendanceDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Interfaces.IServices.System
{
    public interface IAttendanceService
    {
        Task<AttendanceStatusDto> GetMyStatusAsync(DateOnly date);
        Task<AttendanceDto> CheckInAsync(CheckInRequestDto request);
        Task<AttendanceDto> CheckOutAsync(CheckOutRequestDto request);
        Task<PagedResultDto<AttendanceDto>> GetPagedAsync(AttendanceQueryDto query);
        Task<List<AttendanceDto>> GetMyHistoryAsync(DateOnly from, DateOnly to);
        Task<AttendanceDto> GetByIdAsync(Guid id);
        Task<AttendanceDto> ApproveAsync(Guid attendanceId, AttendanceApproveDto dto);
    }
}
