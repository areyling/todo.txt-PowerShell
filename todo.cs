using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class ToDo
{
	public int ItemNumber = 0;
    public String Priority = String.Empty;
	public DateTime? Date = null;
	public String Text;
	public List<String> Contexts = new List<String>();
	public List<String> Projects = new List<String>();
	private bool _completed = false;
	
	public bool Completed
	{
		get{return _completed;}
	}
	
	public void MarkCompleted()
	{
		_completed = true;
		
		if(IsPriority)
		{
			Priority = String.Empty;
		}
		
		Date = DateTime.Now;
	}
	
	public void Empty()
	{
		Text = String.Empty;
		Date = null;
		Priority = String.Empty;
		Contexts = new List<String>();
		Projects = new List<String>();
	}
	
	private String ParseProjects(String todo)
	{
		Projects.Clear();
	
		MatchCollection projects = Regex.Matches(todo, @"\s(\+\w+)");
		
		foreach(Match match in projects)
		{
			String project = match.Groups[1].Captures[0].Value;
			Projects.Add(project);
			todo = todo.Replace(project, String.Empty);
		}
		
		return todo;
	}
	
	private String ParseContexts(String todo)
	{
		Contexts.Clear();
	
		MatchCollection contexts = Regex.Matches(todo, @"\s(@\w+)");
		
		foreach(Match match in contexts)
		{
			String context = match.Groups[1].Captures[0].Value;
			Contexts.Add(context);
			todo = todo.Replace(context, String.Empty);
		}
		
		return todo;
	}
	
	private void ParseEverythingElse(String todo)
	{
		Match everythingElse = Regex.Match(todo, @"(?:(?<done>[x]) )?(?:\((?<priority>[A-Z])\) )?(?:(?<date>[0-9]{4}-[0-9]{2}-[0-9]{2}) )?(?<todo>.+)$");
		
		if(everythingElse != Match.Empty)
		{
			if(everythingElse.Groups["date"].Success)
			{
				Date = DateTime.Parse(everythingElse.Groups["date"].Value);
			}
			
			if(everythingElse.Groups["priority"].Success)
			{
				Priority = everythingElse.Groups["priority"].Value;
			}
			
			if(everythingElse.Groups["todo"].Success)
			{
				Text = everythingElse.Groups["todo"].Value;
			}
			
			if(everythingElse.Groups["done"].Success)
			{
				_completed = true;
			}
		}
	}
	
	public ToDo(String todo, int itemNumber)
	{
		ItemNumber = itemNumber;
		
		ParseFields(todo);
	}
	
	private void ParseFields(String todo)
	{
		todo = ParseContexts(todo);
		todo = ParseProjects(todo);
		
		todo = todo.Trim();
		
		ParseEverythingElse(todo);
	}
	
	public void Replace(String newTodo)
	{
		ParseFields(newTodo);
	}
	
	public void Append(String toAppend)
	{
		ParseFields(ToDoProjectContext + toAppend);
	}

	public void Prepend(String toPrepend)
	{
		ParseFields(toPrepend + ToDoProjectContext);
	}
	
	public bool ReplaceItemText(string oldText, string newText)
	{
		String replaceableText = ToDoProjectContext;
	
		if(replaceableText.Contains(oldText))
		{
			replaceableText = replaceableText.Replace(oldText, newText);
			ParseFields(replaceableText);
			return true;
		}
		
		return false;
	}
	
	public bool IsPriority
	{
		get{ return !String.IsNullOrEmpty(Priority); }
	}
	
	private String ToDoProjectContext
	{
		get
		{
			return Text 
			+ (Projects.Count() > 0 ? " " : String.Empty)
			+ String.Join(" ", Projects.ToArray()) 
			+ (Contexts.Count() > 0 ? " " : String.Empty) 
			+ String.Join(" ", Contexts.ToArray());
		}
	}
	
	public String ToString(String numberFormat)
	{
		return ItemNumber.ToString(numberFormat) + " " + ToString();
	}
	
	public override String ToString()
	{
		return 
			(_completed ? "x " : String.Empty)
			+ (!String.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : String.Empty)
			+ (Date.HasValue ? (Date.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
			+ ToDoProjectContext;
	}
}

public class ToDoList : List<ToDo>
{
	private String _numberFormat;

	public ToDoList() : base()
	{}
	
	public ToDoList(IEnumerable<ToDo> todos, int parentListItemCount)
		: base(todos)
	{
		_numberFormat = new String('0', parentListItemCount.ToString().Length);
	}
	
	public IEnumerable<String> ToOutput()
	{
		return this.Select(x => x.ToString());
	}
	
	public IEnumerable<String> ToNumberedOutput()
	{
		if(String.IsNullOrEmpty(_numberFormat))
		{
			_numberFormat = new String('0', Count.ToString().Length);
		}
	
		return this.Select(x => x.ToString(_numberFormat));
	}
	
	public ToDoList ListCompleted()
	{
		return new ToDoList(from todo in this
				where todo.Completed
				select todo, Count);
	}
	
	public ToDoList Search(String term)
	{
		bool include = true;
		
		if(term.StartsWith("-"))
		{
			include = false;
			term = term.Substring(1);
		}
			
		return new ToDoList(from todo in this
				where !(include ^ todo.ToString().Contains(term))
				select todo, Count);
	}	
	
	public ToDoList GetPriority(String priority)
	{
		if(!String.IsNullOrEmpty(priority))
		{
			return new ToDoList(from todo in this
				where todo.Priority == priority
				select todo, Count);
		}
		else
		{
			return new ToDoList(from todo in this
				where todo.IsPriority
				orderby todo.Priority
				select todo, Count);
		}
	}
	
	public void SetItemPriority(int item, string priority)
	{
		ToDo target = (from todo in this
						where todo.ItemNumber == item
						select todo).FirstOrDefault();
						
		if(target != null)
		{
			target.Priority = priority;
		}
	}
	
	private bool ReplaceItemText(int item, string oldText, string newText)
	{
		ToDo target = (from todo in this
						where todo.ItemNumber == item
						select todo).FirstOrDefault();
						
		if(target != null)
		{
			return target.ReplaceItemText(oldText, newText);
		}
		
		return false;
	}
	
	public void ReplaceToDo(int item, string newText)
	{
		ToDo target = (from todo in this
						where todo.ItemNumber == item
						select todo).FirstOrDefault();
						
		if(target != null)
		{
			target.Replace(newText);
		}			
	}
	
	public void AppendToDo(int item, string newText)
	{
		ToDo target = (from todo in this
						where todo.ItemNumber == item
						select todo).FirstOrDefault();
						
		if(target != null)
		{
			target.Append(newText);
		}			
	}
	
	public void PrependToDo(int item, string newText)
	{
		ToDo target = (from todo in this
						where todo.ItemNumber == item
						select todo).FirstOrDefault();
						
		if(target != null)
		{
			target.Prepend(newText);
		}			
	}
	
	public bool RemoveFromItem(int item, string term)
	{
		return ReplaceItemText(item, term, String.Empty);
	}
	
	public ToDoList RemoveCompletedItems(bool preserveLineNumbers)
	{
		ToDoList completed = ListCompleted();
		
		for(int n = this.Count - 1; n >= 0; n--)
		{
			if(this[n].Completed)
			{
				if(preserveLineNumbers)
				{
					this[n].Empty();
				}
				else
				{
					Remove(this[n]);
				}
			}
		}
		
		return completed;
	}
	
	public void RemoveItem(int item, bool preserveLineNumbers)
	{
		ToDo target = (from todo in this
						where todo.ItemNumber == item
						select todo).FirstOrDefault();
			
		if(target != null)
		{			
			if(preserveLineNumbers)
			{
				target.Empty();
			}
			else
			{
				this.Remove(target);
				
				int itemNumber = 1;
				foreach(ToDo todo in this)
				{
					todo.ItemNumber = itemNumber;
					itemNumber += 1;
				}
			}
		}
	}
}