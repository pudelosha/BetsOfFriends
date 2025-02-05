import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyBetsPlacedPage } from './my-bets-placed.page';

describe('MyBetsPlacedPage', () => {
  let component: MyBetsPlacedPage;
  let fixture: ComponentFixture<MyBetsPlacedPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(MyBetsPlacedPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
