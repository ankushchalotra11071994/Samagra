 

 table Employee duplicate on Email


 With deletedduplicate as
 (
    Select *, Row_number() over( partition by(Email) order by email) as rank
    from  Employee
 ) 
 delete from deletedduplicate where rank >1