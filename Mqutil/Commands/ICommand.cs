using System.Threading.Tasks;

namespace Mqutil.Commands
{
    public interface ICommand
    {
        Task Run();
    }
}
