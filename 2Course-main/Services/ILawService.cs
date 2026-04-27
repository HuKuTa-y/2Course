using System.Collections.Generic;
using System.Threading.Tasks;
using _2course.Models;

namespace _2course.Services
{
	public interface ILawService
	{
		Task<List<Codek>> GetCodeksAsync();
		Task<List<Law>> GetLawsAsync();
		Task<List<ArticleFull>> GetArticlesFullAsync(); // ? Добавлено
		Task<List<ArticleFull>> GetArticlesBySourceAsync(string sourceNumber);
		Task<TextArticle> GetArticleTextAsync(string articleName);
		Task<List<ArticleFull>> SearchByNumberAsync(int number);
		Task<List<ArticleFull>> SearchByTextAsync(string query);
		List<ArticleFull> GetLoadedArticlesFull();
		Task RefreshAllDataAsync();
	}
}