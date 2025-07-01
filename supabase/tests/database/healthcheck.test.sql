begin;

select plan(1);

select ok( (select 1), 'Basic arithmetic still works ▢');

select * from finish();
rollback;
