using System.Text.Json;

namespace GitHubBot_LagrangeV1.Services;

public class GitHubService
{
    private readonly HttpClient _httpClient;
    private readonly string? _token;
    
    public GitHubService(HttpClient httpClient, string? token = null)
    {
        _httpClient = httpClient;
        _token = token;
        
        // 设置GitHub API的基本请求头
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubBot-LagrangeV1");
        if (!string.IsNullOrEmpty(_token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_token}");
        }
    }

    /// <summary>
    /// 获取仓库的最新提交
    /// </summary>
    public async Task<List<GitHubCommit>> GetLatestCommitsAsync(string owner, string repo, int count = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{owner}/{repo}/commits?per_page={count}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var commits = JsonSerializer.Deserialize<List<GitHubCommit>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return commits ?? new List<GitHubCommit>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取提交信息失败: {ex.Message}");
        }
        
        return new List<GitHubCommit>();
    }

    /// <summary>
    /// 获取仓库的最新Issues
    /// </summary>
    public async Task<List<GitHubIssue>> GetLatestIssuesAsync(string owner, string repo, int count = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{owner}/{repo}/issues?state=all&per_page={count}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var issues = JsonSerializer.Deserialize<List<GitHubIssue>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return issues ?? new List<GitHubIssue>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取Issues信息失败: {ex.Message}");
        }
        
        return new List<GitHubIssue>();
    }

    /// <summary>
    /// 获取仓库的最新Releases
    /// </summary>
    public async Task<List<GitHubRelease>> GetLatestReleasesAsync(string owner, string repo, int count = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{owner}/{repo}/releases?per_page={count}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return releases ?? new List<GitHubRelease>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取Releases信息失败: {ex.Message}");
        }
        
        return new List<GitHubRelease>();
    }

    /// <summary>
    /// 获取仓库信息
    /// </summary>
    public async Task<GitHubRepository?> GetRepositoryAsync(string owner, string repo)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{owner}/{repo}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var repository = JsonSerializer.Deserialize<GitHubRepository>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return repository;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取仓库信息失败: {ex.Message}");
        }
        
        return null;
    }
}

// GitHub数据模型
public class GitHubCommit
{
    public string Sha { get; set; } = string.Empty;
    public CommitInfo Commit { get; set; } = new();
    public GitHubUser Author { get; set; } = new();
    public string HtmlUrl { get; set; } = string.Empty;
}

public class CommitInfo
{
    public string Message { get; set; } = string.Empty;
    public CommitAuthor Author { get; set; } = new();
    public DateTime? Date => Author.Date;
}

public class CommitAuthor
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class GitHubIssue
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public GitHubUser User { get; set; } = new();
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GitHubRelease
{
    public string TagName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool Draft { get; set; }
    public bool Prerelease { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PublishedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public GitHubUser Author { get; set; } = new();
}

public class GitHubUser
{
    public string Login { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
}

public class GitHubRepository
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public int StargazersCount { get; set; }
    public int ForksCount { get; set; }
    public string Language { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public GitHubUser Owner { get; set; } = new();
}