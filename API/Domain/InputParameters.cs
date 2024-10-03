namespace API.Domain;
public class InputParameters
{
    public List<string> Titles { get; set; }

    public InputParameters(List<string> titles)
    {
        Titles = titles;
    }
    public InputParameters()
    {
    }
}
