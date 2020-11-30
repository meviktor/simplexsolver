import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LpModelCreatorComponent } from './lp-model-creator.component';

describe('LpModelCreatorComponent', () => {
  let component: LpModelCreatorComponent;
  let fixture: ComponentFixture<LpModelCreatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LpModelCreatorComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LpModelCreatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
