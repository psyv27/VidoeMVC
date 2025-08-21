using vidoeMVC.ViewModels.Users;
using vidoeMVC.Enums;

namespace vidoeMVC.ViewModels.Videos
{
    
        public class VideoViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; }
        public string Description { get; set; }
            public string TumbnailUrl { get; set; }
            public string VideoUrl { get; set; }
            public DateTime CreatedTime { get; set; }
            public string Duration { get; set; } 
            public UserVM Author { get; set; }
            public int ViewCount { get; set; }
            public List<VideoStatus>? Privacy { get; set; }
           
        }

    
}
