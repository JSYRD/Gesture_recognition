using System;
using N1;
namespace N1
{
    class Program
    { 
        static void Main(string []args)
        {
            Console.WriteLine("Hello world!");
            Student student = new Student(10205102454, "YangRundong");
            Console.WriteLine("{0}, {1}",student.Id, student.Name);
            try
            {
                int a = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("{0}", a);
            }
            catch (Exception)
            {
                Console.WriteLine("Please enter a number!");
                throw;
            }
        }
    }
    class Student
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public Student(long Id, string Name)
        {
            this.Id = Id;
            this.Name = Name;
        }
    }
}
